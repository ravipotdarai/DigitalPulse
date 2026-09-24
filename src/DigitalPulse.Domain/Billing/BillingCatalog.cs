using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Billing;

public enum BillingInterval
{
    Monthly = 0,
    Annual = 1
}

public enum InvoiceStatus
{
    Draft = 0,
    Issued = 1,
    Held = 2,
    Paid = 3,
    Failed = 4,
    Void = 5
}

public enum PaymentAttemptStatus
{
    Pending = 0,
    Held = 1,
    Succeeded = 2,
    Failed = 3
}

public enum UsageKind
{
    Scan = 0,
    Action = 1,
    AiGeneration = 2,
    WhatsAppMessage = 3,
    StorageMb = 4
}

public static class BillingPolicy
{
    public static bool IsUsable(SubscriptionStatus status) =>
        status is SubscriptionStatus.Active or SubscriptionStatus.Trial;

    public static DateTimeOffset PeriodStart(DateTimeOffset? now = null)
    {
        var at = now ?? DateTimeOffset.UtcNow;
        return new DateTimeOffset(at.Year, at.Month, 1, 0, 0, 0, TimeSpan.Zero);
    }

    public static DateTimeOffset PeriodEnd(BillingInterval interval, DateTimeOffset? start = null)
    {
        var from = start ?? PeriodStart();
        return interval == BillingInterval.Annual ? from.AddYears(1).AddTicks(-1) : from.AddMonths(1).AddTicks(-1);
    }

    public static decimal PriceFor(SubscriptionPlan plan, BillingInterval interval) =>
        interval == BillingInterval.Annual
            ? (plan.AnnualPriceInr > 0 ? plan.AnnualPriceInr : plan.MonthlyPriceInr * 10)
            : plan.MonthlyPriceInr;

    public static int LocationCap(SubscriptionPlan plan) => plan.MaxLocations < 1 ? 1 : plan.MaxLocations;

    public static int BusinessCap(Tenancy.TenantType tenantType, SubscriptionPlan plan)
    {
        if (tenantType == Tenancy.TenantType.Agency && plan.MaxAgencyClients > 0)
        {
            return Math.Min(plan.MaxBusinesses, plan.MaxAgencyClients);
        }

        return plan.MaxBusinesses;
    }
}

public sealed class Invoice : TenantOwnedEntity
{
    public Guid SubscriptionId { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Issued;
    public BillingInterval Interval { get; private set; }
    public decimal AmountInr { get; private set; }
    public DateTimeOffset PeriodStartUtc { get; private set; }
    public DateTimeOffset PeriodEndUtc { get; private set; }
    public DateTimeOffset IssuedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAtUtc { get; private set; }
    public string HoldReason { get; private set; } = string.Empty;

    private Invoice() { }

    public static Invoice Issue(
        Guid tenantId,
        Guid subscriptionId,
        string number,
        BillingInterval interval,
        decimal amountInr,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        string holdReason) =>
        new()
        {
            TenantId = tenantId,
            SubscriptionId = subscriptionId,
            Number = number.Trim(),
            Status = InvoiceStatus.Held,
            Interval = interval,
            AmountInr = amountInr < 0 ? 0 : amountInr,
            PeriodStartUtc = periodStart,
            PeriodEndUtc = periodEnd,
            IssuedAtUtc = DateTimeOffset.UtcNow,
            HoldReason = holdReason.Trim()
        };

    public void MarkPaid(string detail)
    {
        Status = InvoiceStatus.Paid;
        PaidAtUtc = DateTimeOffset.UtcNow;
        HoldReason = detail.Trim();
        Touch();
    }

    public void MarkFailed(string detail)
    {
        Status = InvoiceStatus.Failed;
        HoldReason = detail.Trim();
        Touch();
    }

    public void Void(string detail)
    {
        Status = InvoiceStatus.Void;
        HoldReason = detail.Trim();
        Touch();
    }
}

public sealed class InvoiceLine : TenantOwnedEntity
{
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal AmountInr { get; private set; }

    private InvoiceLine() { }

    public static InvoiceLine Create(Guid tenantId, Guid invoiceId, string description, decimal amountInr) =>
        new()
        {
            TenantId = tenantId,
            InvoiceId = invoiceId,
            Description = description.Trim(),
            AmountInr = amountInr
        };
}

public sealed class PaymentAttempt : TenantOwnedEntity
{
    public Guid InvoiceId { get; private set; }
    public PaymentAttemptStatus Status { get; private set; } = PaymentAttemptStatus.Held;
    public string Provider { get; private set; } = string.Empty;
    public string? ProviderReference { get; private set; }
    public string Detail { get; private set; } = string.Empty;

    private PaymentAttempt() { }

    public static PaymentAttempt Record(
        Guid tenantId,
        Guid invoiceId,
        PaymentAttemptStatus status,
        string provider,
        string? providerReference,
        string detail) =>
        new()
        {
            TenantId = tenantId,
            InvoiceId = invoiceId,
            Status = status,
            Provider = provider.Trim(),
            ProviderReference = string.IsNullOrWhiteSpace(providerReference) ? null : providerReference.Trim(),
            Detail = detail.Trim()
        };
}

public sealed class UsageRecord : TenantOwnedEntity
{
    public UsageKind Kind { get; private set; }
    public int Quantity { get; private set; } = 1;
    public DateTimeOffset PeriodStartUtc { get; private set; } = BillingPolicy.PeriodStart();
    public string Source { get; private set; } = string.Empty;

    private UsageRecord() { }

    public static UsageRecord Record(Guid tenantId, UsageKind kind, int quantity, string source) =>
        new()
        {
            TenantId = tenantId,
            Kind = kind,
            Quantity = quantity < 1 ? 1 : quantity,
            PeriodStartUtc = BillingPolicy.PeriodStart(),
            Source = source.Trim()
        };
}

public sealed class BillingWebhookEvent : Entity
{
    public Guid? TenantId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public bool SignatureValid { get; private set; }
    public bool Untrusted { get; private set; } = true;
    public bool Processed { get; private set; }
    public string HoldReason { get; private set; } = string.Empty;

    private BillingWebhookEvent() { }

    public static BillingWebhookEvent Ingest(
        string provider,
        string eventType,
        string payload,
        bool signatureValid,
        string holdReason,
        Guid? tenantId = null) =>
        new()
        {
            Provider = provider.Trim(),
            EventType = string.IsNullOrWhiteSpace(eventType) ? "unknown" : eventType.Trim(),
            Payload = payload.Length <= 4000 ? payload : payload[..4000],
            SignatureValid = signatureValid,
            Untrusted = !signatureValid,
            TenantId = tenantId,
            HoldReason = holdReason.Trim()
        };

    public void MarkProcessed(Guid? tenantId, string detail)
    {
        Processed = true;
        Untrusted = false;
        TenantId = tenantId ?? TenantId;
        HoldReason = detail.Trim();
        Touch();
    }
}
