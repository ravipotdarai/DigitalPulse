using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Contracts.Billing;
using DigitalPulse.Domain.Ai;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Billing;

internal static class BillingMaps
{
    public static BillingInterval ParseInterval(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("Monthly", StringComparison.OrdinalIgnoreCase))
        {
            return BillingInterval.Monthly;
        }

        if (value.Equals("Annual", StringComparison.OrdinalIgnoreCase))
        {
            return BillingInterval.Annual;
        }

        throw AppException.Validation("Interval must be Monthly or Annual.");
    }

    public static PlanResponse ToResponse(this SubscriptionPlan plan) =>
        new(
            plan.Id,
            plan.Code,
            plan.Name,
            plan.MonthlyPriceInr,
            plan.AnnualPriceInr,
            plan.MaxBusinesses,
            plan.MaxLocations,
            plan.MaxConnections,
            plan.ScansPerMonth,
            plan.ActionsPerMonth,
            plan.AiGenerationsPerMonth,
            plan.MaxUsers,
            plan.MaxAgencyClients,
            plan.StorageGb,
            plan.WhiteLabel,
            plan.WhatsAppEnabled,
            plan.WhatsAppMessagesPerMonth,
            plan.MonitoringIntervalHours,
            plan.AgencyOnly);

    public static SubscriptionResponse ToResponse(this Subscription subscription, SubscriptionPlan plan) =>
        new(
            subscription.Id,
            subscription.TenantId,
            plan.Code,
            plan.Name,
            plan.MonthlyPriceInr,
            plan.MaxBusinesses,
            subscription.Status.ToString(),
            subscription.Interval.ToString(),
            subscription.PeriodStartUtc,
            subscription.PeriodEndUtc,
            subscription.CancelAtPeriodEnd,
            subscription.HoldReason);
}

internal static class UsageMeter
{
    public static Task RecordAsync(IAppDbContext db, Guid tenantId, UsageKind kind, string source, CancellationToken cancellationToken)
    {
        db.UsageRecords.Add(UsageRecord.Record(tenantId, kind, 1, source));
        return Task.CompletedTask;
    }

    public static Task<int> UsedAsync(IAppDbContext db, Guid tenantId, UsageKind kind, CancellationToken cancellationToken)
    {
        var start = BillingPolicy.PeriodStart();
        return db.UsageRecords.CountAsync(u => u.TenantId == tenantId && u.Kind == kind && u.PeriodStartUtc == start, cancellationToken);
    }
}

internal static class BillingStore
{
    public static async Task<Subscription?> CurrentAsync(IAppDbContext db, Guid tenantId, CancellationToken cancellationToken) =>
        await db.Subscriptions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public static async Task IssueHeldInvoiceAsync(
        IAppDbContext db,
        Subscription subscription,
        SubscriptionPlan plan,
        string reason,
        CancellationToken cancellationToken)
    {
        var count = await db.Invoices.CountAsync(i => i.TenantId == subscription.TenantId, cancellationToken);
        var invoice = Invoice.Issue(
            subscription.TenantId,
            subscription.Id,
            $"INV-{DateTime.UtcNow:yyyyMM}-{count + 1:00}",
            subscription.Interval,
            BillingPolicy.PriceFor(plan, subscription.Interval),
            subscription.PeriodStartUtc,
            subscription.PeriodEndUtc,
            reason);
        db.Invoices.Add(invoice);
        db.InvoiceLines.Add(InvoiceLine.Create(
            subscription.TenantId,
            invoice.Id,
            $"{plan.Name} {subscription.Interval.ToString().ToLowerInvariant()} subscription",
            invoice.AmountInr));
    }

    public static async Task<BillingWorkspaceResponse> LoadAsync(
        IAppDbContext db,
        Guid tenantId,
        TenantType tenantType,
        IBillingGateway gateway,
        IReadOnlyList<SubscriptionPlan> available,
        CancellationToken cancellationToken)
    {
        var subscription = await CurrentAsync(db, tenantId, cancellationToken);
        var plan = subscription is null
            ? null
            : await db.Plans.AsNoTracking().FirstAsync(p => p.Id == subscription.PlanId, cancellationToken);
        var invoices = await db.Invoices.AsNoTracking()
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.IssuedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);
        var lines = await db.InvoiceLines.AsNoTracking()
            .Where(l => invoices.Select(i => i.Id).Contains(l.InvoiceId))
            .ToListAsync(cancellationToken);
        var payments = await db.PaymentAttempts.AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);
        var webhooks = await db.BillingWebhookEvents.AsNoTracking()
            .Where(w => w.TenantId == tenantId)
            .OrderByDescending(w => w.CreatedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);

        var start = BillingPolicy.PeriodStart();
        var scans = await db.Scans.CountAsync(s => s.TenantId == tenantId && s.CreatedAtUtc >= start, cancellationToken);
        var actions = await db.WorkActions.CountAsync(a => a.TenantId == tenantId && a.CreatedAtUtc >= start, cancellationToken);
        var ai = await db.AiRuns.CountAsync(r => r.TenantId == tenantId && r.CreatedAtUtc >= start && r.Status == AiRunStatus.Completed, cancellationToken);
        var whatsApp = await db.WhatsAppMessages.CountAsync(
            m => m.TenantId == tenantId && m.CreatedAtUtc >= start && m.Kind != Domain.WhatsApp.WhatsAppMessageKind.Inbound,
            cancellationToken);
        var usage = plan is null
            ? Array.Empty<UsageMeterResponse>()
            : new UsageMeterResponse[]
            {
                new("Scans", scans, plan.ScansPerMonth, "Counted from stored DigitalPulse Check runs this month."),
                new("Actions", actions, plan.ActionsPerMonth, "Counted from queued work actions this month."),
                new("AI generations", ai, plan.AiGenerationsPerMonth, "Counted from completed orchestrator runs this month."),
                new("WhatsApp messages", whatsApp, plan.WhatsAppMessagesPerMonth, "Inbound replies are not billed against the monthly cap."),
                new("Storage GB", 0, plan.StorageGb, "Hosted object storage is not inventoried. Media catalog entries are not billed as bytes.")
            };

        return new BillingWorkspaceResponse(
            subscription?.ToResponse(plan!),
            plan?.ToResponse(),
            gateway.IsLive,
            gateway.ProviderName,
            "Plans and entitlements come from the SQL catalog. Checkout stays held unless an official billing provider confirms a capture. Webhooks without a valid signature stay untrusted.",
            available.Select(p => p.ToResponse()).ToList(),
            usage,
            invoices.Select(i => new InvoiceResponse(
                i.Id,
                i.Number,
                i.Status.ToString(),
                i.Interval.ToString(),
                i.AmountInr,
                i.PeriodStartUtc,
                i.PeriodEndUtc,
                i.IssuedAtUtc,
                i.PaidAtUtc,
                i.HoldReason,
                lines.Where(l => l.InvoiceId == i.Id).Select(l => l.Description).ToList())).ToList(),
            payments.Select(p => new PaymentAttemptResponse(p.Id, p.Status.ToString(), p.Provider, p.ProviderReference, p.Detail, p.CreatedAtUtc)).ToList(),
            webhooks.Select(w => new BillingWebhookResponse(w.Id, w.Provider, w.EventType, w.SignatureValid, w.Untrusted, w.Processed, w.HoldReason)).ToList());
    }
}

public sealed class GetBillingWorkspaceHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ISubscriptionPlanCatalog _catalog;
    private readonly IBillingGateway _gateway;

    public GetBillingWorkspaceHandler(
        IAppDbContext db,
        ITenantContext tenant,
        ISubscriptionPlanCatalog catalog,
        IBillingGateway gateway)
    {
        _db = db;
        _tenant = tenant;
        _catalog = catalog;
        _gateway = gateway;
    }

    public async Task<BillingWorkspaceResponse> Handle(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var tenant = await _db.Tenants.AsNoTracking().FirstAsync(t => t.Id == tenantId, cancellationToken);
        var available = await _catalog.ListForTenantTypeAsync(tenant.Type, cancellationToken);
        return await BillingStore.LoadAsync(_db, tenantId, tenant.Type, _gateway, available, cancellationToken);
    }
}

public sealed class ChangePlanHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ISubscriptionPlanCatalog _catalog;
    private readonly IBillingGateway _gateway;

    public ChangePlanHandler(IAppDbContext db, ITenantContext tenant, ISubscriptionPlanCatalog catalog, IBillingGateway gateway)
    {
        _db = db;
        _tenant = tenant;
        _catalog = catalog;
        _gateway = gateway;
    }

    public async Task<BillingWorkspaceResponse> Handle(ChangePlanRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var tenant = await _db.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
        var plan = await _catalog.GetByCodeAsync(request.PlanCode, cancellationToken)
            ?? throw AppException.NotFound("Subscription plan was not found.");
        try
        {
            Subscription.EnsurePlanMatchesTenant(tenant.Type, plan);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        var subscription = await BillingStore.CurrentAsync(_db, tenantId, cancellationToken)
            ?? throw AppException.Validation("Select a plan before changing it.");
        try
        {
            subscription.ChangePlan(plan.Id, BillingMaps.ParseInterval(request.Interval));
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        await BillingStore.IssueHeldInvoiceAsync(
            _db,
            subscription,
            plan,
            "Plan change invoice. Payment is not captured without a live billing provider.",
            cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        var available = await _catalog.ListForTenantTypeAsync(tenant.Type, cancellationToken);
        return await BillingStore.LoadAsync(_db, tenantId, tenant.Type, _gateway, available, cancellationToken);
    }
}

public sealed class CheckoutBillingHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IBillingGateway _gateway;
    private readonly ISubscriptionPlanCatalog _catalog;

    public CheckoutBillingHandler(IAppDbContext db, ITenantContext tenant, IBillingGateway gateway, ISubscriptionPlanCatalog catalog)
    {
        _db = db;
        _tenant = tenant;
        _gateway = gateway;
        _catalog = catalog;
    }

    public async Task<BillingWorkspaceResponse> Handle(CheckoutRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var tenant = await _db.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
        var subscription = await BillingStore.CurrentAsync(_db, tenantId, cancellationToken)
            ?? throw AppException.Validation("Select a plan before checkout.");
        if (!subscription.IsUsable && subscription.Status != SubscriptionStatus.PastDue)
        {
            throw AppException.Validation("This subscription cannot be checked out.");
        }

        var invoice = request.InvoiceId is { } id
            ? await _db.Invoices.FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId, cancellationToken)
            : await _db.Invoices.Where(i => i.TenantId == tenantId && i.Status == InvoiceStatus.Held)
                .OrderByDescending(i => i.IssuedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        if (invoice is null)
        {
            throw AppException.Validation("No held invoice is ready for checkout.");
        }

        var plan = await _db.Plans.AsNoTracking().FirstAsync(p => p.Id == subscription.PlanId, cancellationToken);
        var result = await _gateway.CheckoutAsync(plan.Code, invoice.AmountInr, invoice.Number, cancellationToken);
        var status = result.Status.Equals("Succeeded", StringComparison.OrdinalIgnoreCase)
            ? PaymentAttemptStatus.Succeeded
            : result.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase)
                ? PaymentAttemptStatus.Failed
                : PaymentAttemptStatus.Held;
        _db.PaymentAttempts.Add(PaymentAttempt.Record(tenantId, invoice.Id, status, result.Provider, result.Reference, result.Detail));
        if (status == PaymentAttemptStatus.Succeeded)
        {
            invoice.MarkPaid(result.Detail);
        }

        subscription.AttachProvider(result.Provider, result.Reference);
        await _db.SaveChangesAsync(cancellationToken);
        var available = await _catalog.ListForTenantTypeAsync(tenant.Type, cancellationToken);
        return await BillingStore.LoadAsync(_db, tenantId, tenant.Type, _gateway, available, cancellationToken);
    }
}

public sealed class CancelSubscriptionHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IBillingGateway _gateway;
    private readonly ISubscriptionPlanCatalog _catalog;

    public CancelSubscriptionHandler(IAppDbContext db, ITenantContext tenant, IBillingGateway gateway, ISubscriptionPlanCatalog catalog)
    {
        _db = db;
        _tenant = tenant;
        _gateway = gateway;
        _catalog = catalog;
    }

    public async Task<BillingWorkspaceResponse> Handle(CancelSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var tenant = await _db.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
        var subscription = await BillingStore.CurrentAsync(_db, tenantId, cancellationToken)
            ?? throw AppException.Validation("No subscription to cancel.");
        try
        {
            if (request.Immediately)
            {
                subscription.CancelNow(request.Reason ?? "Cancelled immediately. Metered work is blocked.");
            }
            else
            {
                subscription.RequestCancel();
            }
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        var available = await _catalog.ListForTenantTypeAsync(tenant.Type, cancellationToken);
        return await BillingStore.LoadAsync(_db, tenantId, tenant.Type, _gateway, available, cancellationToken);
    }
}

public sealed class ResumeSubscriptionHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IBillingGateway _gateway;
    private readonly ISubscriptionPlanCatalog _catalog;

    public ResumeSubscriptionHandler(IAppDbContext db, ITenantContext tenant, IBillingGateway gateway, ISubscriptionPlanCatalog catalog)
    {
        _db = db;
        _tenant = tenant;
        _gateway = gateway;
        _catalog = catalog;
    }

    public async Task<BillingWorkspaceResponse> Handle(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var tenant = await _db.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
        var subscription = await BillingStore.CurrentAsync(_db, tenantId, cancellationToken)
            ?? throw AppException.Validation("No subscription to resume.");
        try
        {
            subscription.Resume();
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        var available = await _catalog.ListForTenantTypeAsync(tenant.Type, cancellationToken);
        return await BillingStore.LoadAsync(_db, tenantId, tenant.Type, _gateway, available, cancellationToken);
    }
}

public sealed class RecordBillingWebhookHandler
{
    private readonly IAppDbContext _db;
    private readonly IBillingGateway _gateway;

    public RecordBillingWebhookHandler(IAppDbContext db, IBillingGateway gateway)
    {
        _db = db;
        _gateway = gateway;
    }

    public async Task<BillingWebhookResponse> Handle(BillingWebhookIngest request, CancellationToken cancellationToken)
    {
        var payload = request.Payload ?? string.Empty;
        var valid = _gateway.VerifyWebhook(request.Signature, payload);
        var evt = BillingWebhookEvent.Ingest(
            _gateway.ProviderName,
            request.EventType ?? "unknown",
            payload,
            valid,
            valid
                ? "Signature verified. Provider state is applied only for known subscription references."
                : "Webhook is untrusted. A valid provider signature is required before DigitalPulse changes subscription state.");
        _db.BillingWebhookEvents.Add(evt);

        if (valid && !string.IsNullOrWhiteSpace(request.EventType))
        {
            var reference = request.EventType;
            var subscription = await _db.Subscriptions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.ProviderSubscriptionId != null && payload.Contains(s.ProviderSubscriptionId), cancellationToken);
            if (subscription is not null && request.EventType.Contains("paid", StringComparison.OrdinalIgnoreCase))
            {
                var invoice = await _db.Invoices.IgnoreQueryFilters()
                    .Where(i => i.SubscriptionId == subscription.Id && i.Status == InvoiceStatus.Held)
                    .OrderByDescending(i => i.IssuedAtUtc)
                    .FirstOrDefaultAsync(cancellationToken);
                invoice?.MarkPaid("Live billing webhook marked the invoice paid.");
                if (subscription.Status == SubscriptionStatus.PastDue)
                {
                    subscription.Resume();
                }

                evt.MarkProcessed(subscription.TenantId, "Applied a verified paid event.");
            }
            else if (subscription is not null && request.EventType.Contains("failed", StringComparison.OrdinalIgnoreCase))
            {
                subscription.MarkPastDue("Live billing webhook reported a failed payment.");
                evt.MarkProcessed(subscription.TenantId, "Applied a verified failed event.");
            }
            else
            {
                evt.MarkProcessed(subscription?.TenantId, $"Verified {reference}. No matching subscription reference was stored, so entitlements did not change.");
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new BillingWebhookResponse(evt.Id, evt.Provider, evt.EventType, evt.SignatureValid, evt.Untrusted, evt.Processed, evt.HoldReason);
    }
}
