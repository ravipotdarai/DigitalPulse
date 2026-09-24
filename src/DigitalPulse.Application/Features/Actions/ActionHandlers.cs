using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Ai;
using DigitalPulse.Application.Features.Directories;
using DigitalPulse.Application.Features.Identity;
using DigitalPulse.Application.Features.Scans;
using DigitalPulse.Application.Features.Social;
using DigitalPulse.Application.Features.Website;
using DigitalPulse.Application.Features.WhatsApp;
using DigitalPulse.Application.Features.Monitoring;
using DigitalPulse.Contracts.Actions;
using DigitalPulse.Contracts.Directories;
using DigitalPulse.Domain.Actions;
using DigitalPulse.Domain.Billing;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Actions;

internal static class ActionMaps
{
    public static AutomationMode ParseMode(string value) =>
        Enum.TryParse<AutomationMode>(value, true, out var parsed)
            ? parsed
            : throw AppException.Validation("Mode must be Assisted or FullAuto.");

    public static ActionKindResponse ToResponse(this ActionKindDescriptor kind) =>
        new(kind.Code, kind.Name, kind.Risk.ToString(), kind.ExternalWrite, kind.Purpose);

    public static AutomationPolicyResponse ToResponse(this AutomationPolicy policy) =>
        new(policy.Mode.ToString(), policy.AllowLowRiskAuto, policy.RequireApprovalForHighRisk, policy.MaxAttempts);

    public static WorkActionResponse ToResponse(
        this WorkAction action,
        IReadOnlyList<ActionAttempt> attempts,
        ActionVerification? verification) =>
        new(
            action.Id,
            ActionKindCatalog.Of(action.Kind).Code,
            action.Status.ToString(),
            action.Risk.ToString(),
            action.Title,
            action.IdempotencyKey,
            action.TargetId,
            action.TargetLabel,
            action.LiveWriteAvailable,
            action.AutopilotEligible,
            action.HoldReason,
            action.AttemptCount,
            action.NextRetryAtUtc,
            action.CreatedAtUtc,
            attempts.Select(a => new ActionAttemptResponse(a.Ordinal, a.Outcome, a.Detail, a.CreatedAtUtc)).ToList(),
            verification is null
                ? null
                : new ActionVerificationResponse(verification.Status.ToString(), verification.Detail, verification.CreatedAtUtc));
}

public sealed class GetActionWorkspaceHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetActionWorkspaceHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ActionWorkspaceResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var policy = await ActionPolicyStore.RequireAsync(_db, tenantId, cancellationToken);
        var plan = await ActionPolicyStore.CurrentPlan(_db, tenantId, cancellationToken);
        var used = await ActionPolicyStore.UsedThisMonthAsync(_db, tenantId, cancellationToken);
        return await ActionWorkspaceLoader.LoadAsync(_db, policy, plan, used, businessId, cancellationToken);
    }
}

public sealed class UpdateAutomationPolicyHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public UpdateAutomationPolicyHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ActionWorkspaceResponse> Handle(Guid businessId, UpdateAutomationPolicyRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var policy = await ActionPolicyStore.RequireAsync(_db, tenantId, cancellationToken);
        try
        {
            policy.Update(ActionMaps.ParseMode(request.Mode), request.AllowLowRiskAuto, request.MaxAttempts);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        if (policy.Mode == AutomationMode.FullAuto && !policy.AllowLowRiskAuto)
        {
            throw AppException.Validation("Full Auto still needs low-risk autopilot enabled. High-risk writes stay approved.");
        }

        await _db.SaveChangesAsync(cancellationToken);
        var plan = await ActionPolicyStore.CurrentPlan(_db, tenantId, cancellationToken);
        var used = await ActionPolicyStore.UsedThisMonthAsync(_db, tenantId, cancellationToken);
        return await ActionWorkspaceLoader.LoadAsync(_db, policy, plan, used, businessId, cancellationToken);
    }
}

public sealed class EnqueueActionHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPlatformAdapterCatalog _catalog;
    private readonly ExecuteActionHandler _execute;

    public EnqueueActionHandler(
        IAppDbContext db,
        ITenantContext tenant,
        IPlatformAdapterCatalog catalog,
        ExecuteActionHandler execute)
    {
        _db = db;
        _tenant = tenant;
        _catalog = catalog;
        _execute = execute;
    }

    public async Task<WorkActionResponse> Handle(Guid businessId, EnqueueActionRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        ActionKindDescriptor kind;
        try
        {
            kind = ActionKindCatalog.Require(request.Kind);
        }
        catch (ArgumentException)
        {
            throw AppException.Validation("Unknown action kind.");
        }

        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length < 3)
        {
            throw AppException.Validation("Give the action a short title.");
        }

        var policy = await ActionPolicyStore.RequireAsync(_db, tenantId, cancellationToken);
        var plan = await ActionPolicyStore.CurrentPlan(_db, tenantId, cancellationToken);
        var used = await ActionPolicyStore.UsedThisMonthAsync(_db, tenantId, cancellationToken);
        try
        {
            EntitlementRules.EnsureCanRunAction(plan, used);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        var liveWrite = kind.ExternalWrite && await LiveWriteAvailableAsync(businessId, kind, request.TargetId, cancellationToken);
        var decision = ActionPolicy.Evaluate(kind.Kind, policy.Mode, liveWrite, alreadyApproved: false);
        if (!policy.AllowLowRiskAuto)
        {
            decision = decision with { EligibleForAutopilot = false, RequiresApproval = true };
        }

        var key = $"{kind.Code}:{request.TargetId?.ToString("N") ?? "workspace"}";
        var existing = await _db.WorkActions.FirstOrDefaultAsync(
            a => a.BusinessId == businessId &&
                 a.IdempotencyKey == key &&
                 a.Status != ActionStatus.Verified &&
                 a.Status != ActionStatus.Escalated,
            cancellationToken);
        if (existing is not null)
        {
            return await ActionWorkspaceLoader.LoadOneAsync(_db, existing.Id, cancellationToken);
        }

        var action = WorkAction.Enqueue(
            tenantId,
            businessId,
            kind,
            request.Title,
            key,
            request.TargetId,
            request.TargetLabel,
            liveWrite,
            decision);
        _db.WorkActions.Add(action);
        await DigitalPulse.Application.Features.Billing.UsageMeter.RecordAsync(_db, tenantId, DigitalPulse.Domain.Billing.UsageKind.Action, kind.Code, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        if (action.Status == ActionStatus.Queued)
        {
            return await _execute.Handle(businessId, action.Id, cancellationToken);
        }

        return await ActionWorkspaceLoader.LoadOneAsync(_db, action.Id, cancellationToken);
    }

    private async Task<bool> LiveWriteAvailableAsync(Guid businessId, ActionKindDescriptor kind, Guid? targetId, CancellationToken cancellationToken)
    {
        if (kind.Kind is ActionKind.SendWhatsAppTemplate or ActionKind.SendWhatsAppSession)
        {
            var whatsApp = await _db.Connections.AsNoTracking()
                .FirstOrDefaultAsync(c => c.BusinessId == businessId && c.PlatformCode == "WHATSAPP", cancellationToken);
            return whatsApp?.HasLiveCredential == true;
        }

        if (kind.Kind != ActionKind.PublishSocial || targetId is null)
        {
            return false;
        }

        var item = await _db.SocialContent.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == targetId && c.BusinessId == businessId, cancellationToken);
        if (item is null)
        {
            return false;
        }

        var caps = _catalog.Get(item.PlatformCode).Describe().Capabilities;
        if (!caps.CanPublish || caps.AssistedOnly)
        {
            return false;
        }

        var connection = await _db.Connections.AsNoTracking()
            .FirstOrDefaultAsync(c => c.BusinessId == businessId && c.PlatformCode == item.PlatformCode, cancellationToken);
        return connection?.HasLiveCredential == true;
    }
}

public sealed class ApproveActionHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public ApproveActionHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<WorkActionResponse> Handle(Guid businessId, Guid actionId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var action = await _db.WorkActions.FirstOrDefaultAsync(a => a.Id == actionId && a.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Action was not found.");
        try
        {
            action.Approve();
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await ActionWorkspaceLoader.LoadOneAsync(_db, action.Id, cancellationToken);
    }
}

public sealed class ExecuteActionHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly AnalyzeWebsiteHandler _website;
    private readonly RunScanHandler _scan;
    private readonly SyncGraphHandler _graph;
    private readonly RefreshSocialMetricsHandler _metrics;
    private readonly PublishSocialContentHandler _publish;
    private readonly MonitorDirectoryHandler _monitor;
    private readonly VerifyDirectoryTaskHandler _verify;
    private readonly SendWhatsAppMessageHandler _whatsApp;
    private readonly RunMonitoringHandler _monitoring;
    private readonly AssemblePresenceReportHandler _report;

    public ExecuteActionHandler(
        IAppDbContext db,
        ITenantContext tenant,
        AnalyzeWebsiteHandler website,
        RunScanHandler scan,
        SyncGraphHandler graph,
        RefreshSocialMetricsHandler metrics,
        PublishSocialContentHandler publish,
        MonitorDirectoryHandler monitor,
        VerifyDirectoryTaskHandler verify,
        SendWhatsAppMessageHandler whatsApp,
        RunMonitoringHandler monitoring,
        AssemblePresenceReportHandler report)
    {
        _db = db;
        _tenant = tenant;
        _website = website;
        _scan = scan;
        _graph = graph;
        _metrics = metrics;
        _publish = publish;
        _monitor = monitor;
        _verify = verify;
        _whatsApp = whatsApp;
        _monitoring = monitoring;
        _report = report;
    }

    public async Task<WorkActionResponse> Handle(Guid businessId, Guid actionId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var policy = await ActionPolicyStore.RequireAsync(_db, tenantId, cancellationToken);
        var action = await _db.WorkActions.FirstOrDefaultAsync(a => a.Id == actionId && a.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Action was not found.");

        try
        {
            action.MarkExecuting();
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var (held, detail) = await DispatchAsync(businessId, action, cancellationToken);
            action.MarkExecuted(detail, held);
            _db.ActionAttempts.Add(ActionAttempt.Record(tenantId, action.Id, action.AttemptCount + 1, held ? "Held" : "Executed", detail));
            _db.ActionVerifications.Add(ActionVerification.Record(
                tenantId,
                action.Id,
                held ? VerificationStatus.Held : VerificationStatus.Pending,
                held
                    ? "Verification waits for a live provider confirmation. Nothing was invented."
                    : "Execution recorded. Confirm against the source system before treating this as verified."));
        }
        catch (Exception ex) when (ex is AppException or InvalidOperationException)
        {
            action.MarkFailed(ex.Message, policy.MaxAttempts);
            _db.ActionAttempts.Add(ActionAttempt.Record(tenantId, action.Id, action.AttemptCount, "Failed", ex.Message));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await ActionWorkspaceLoader.LoadOneAsync(_db, action.Id, cancellationToken);
    }

    private async Task<(bool Held, string Detail)> DispatchAsync(Guid businessId, WorkAction action, CancellationToken cancellationToken)
    {
        switch (action.Kind)
        {
            case ActionKind.AnalyzeWebsite:
                await _website.Handle(businessId, cancellationToken);
                return (false, "Website snapshot stored from a safe fetch. Search Console metrics were not invented.");
            case ActionKind.RunScan:
                await _scan.Handle(businessId, cancellationToken);
                return (false, "DigitalPulse Check ran against the identity record and authorized connections.");
            case ActionKind.RebuildGraphify:
                await _graph.Handle(businessId, cancellationToken);
                return (false, "Graphify rebuilt from the identity record.");
            case ActionKind.RefreshSocialMetrics:
                await _metrics.Handle(businessId, cancellationToken);
                var observed = await _db.SocialMetrics.AsNoTracking()
                    .AnyAsync(m => m.BusinessId == businessId && m.Status == DigitalPulse.Domain.Social.SocialMetricStatus.Observed, cancellationToken);
                return observed
                    ? (false, "Official adapter metrics were stored. Counts were not invented beyond the provider body.")
                    : (true, "Metrics refresh asked the adapter. Development grants stay unavailable.");
            case ActionKind.PublishSocial:
                if (action.TargetId is null)
                {
                    return (true, "Choose an approved social draft before publish.");
                }

                var published = await _publish.Handle(businessId, action.TargetId.Value, cancellationToken);
                var publishHeld = !published.Status.Equals("Published", StringComparison.OrdinalIgnoreCase);
                return (publishHeld, published.LastPublishError ?? published.VerificationDetail ?? "Publish stayed on hold. A live post was not invented.");
            case ActionKind.MonitorDirectory:
                var platform = action.TargetLabel ?? "INDIAMART";
                await _monitor.Handle(businessId, platform, cancellationToken);
                var directory = await _db.Connections.AsNoTracking()
                    .FirstOrDefaultAsync(
                        c => c.BusinessId == businessId && c.PlatformCode == platform.Trim().ToUpperInvariant(),
                        cancellationToken);
                return directory?.HasLiveCredential == true
                    ? (false, "Official directory read stored. Profile writes stay assisted.")
                    : (true, "Directory monitor recorded an assisted observation. Official listing reads were not invented.");
            case ActionKind.VerifyDirectory:
                if (action.TargetId is null)
                {
                    return (true, "Choose a directory task and an operator note before verification.");
                }

                await _verify.Handle(businessId, action.TargetId.Value, new VerifyDirectoryRequest(action.Title), cancellationToken);
                return (true, "Directory verification is operator-confirmed. DigitalPulse did not write to the directory.");
            case ActionKind.RunAi:
                return (true, "Ask the orchestrator with a prompt. The action engine does not invent a model completion.");
            case ActionKind.SendWhatsAppTemplate:
            case ActionKind.SendWhatsAppSession:
                if (action.TargetId is null)
                {
                    return (true, "Choose an approved WhatsApp message before the Cloud API send.");
                }

                var sent = await _whatsApp.Handle(businessId, action.TargetId.Value, cancellationToken);
                var sendHeld = !sent.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase);
                return (sendHeld, sent.HoldReason);
            case ActionKind.RunMonitoring:
                await _monitoring.Handle(businessId, cancellationToken);
                return (false, "Monitoring recorded stored health and honest holds. Live provider metrics were not invented.");
            case ActionKind.AssembleReport:
                await _report.Handle(businessId, cancellationToken);
                return (false, "Report assembled from stored observations. AI interpretation was not invented.");
            default:
                return (true, "Unknown action kind stayed held.");
        }
    }
}

public sealed class RetryActionHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ExecuteActionHandler _execute;

    public RetryActionHandler(IAppDbContext db, ITenantContext tenant, ExecuteActionHandler execute)
    {
        _db = db;
        _tenant = tenant;
        _execute = execute;
    }

    public async Task<WorkActionResponse> Handle(Guid businessId, Guid actionId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var policy = await ActionPolicyStore.RequireAsync(_db, tenantId, cancellationToken);
        var action = await _db.WorkActions.FirstOrDefaultAsync(a => a.Id == actionId && a.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Action was not found.");
        if (!action.CanRetry(policy.MaxAttempts))
        {
            throw AppException.Validation("This action cannot be retried. Escalate or enqueue a new one.");
        }

        return await _execute.Handle(businessId, actionId, cancellationToken);
    }
}

public sealed class VerifyActionHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public VerifyActionHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<WorkActionResponse> Handle(Guid businessId, Guid actionId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var action = await _db.WorkActions.FirstOrDefaultAsync(a => a.Id == actionId && a.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Action was not found.");
        try
        {
            action.MarkVerified(action.LiveWriteAvailable
                ? "Operator verified against the live system."
                : "Operator verified the hold. No live provider result exists to confirm.");
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        _db.ActionVerifications.Add(ActionVerification.Record(
            tenantId,
            action.Id,
            action.LiveWriteAvailable ? VerificationStatus.Verified : VerificationStatus.Held,
            action.HoldReason));
        await _db.SaveChangesAsync(cancellationToken);
        return await ActionWorkspaceLoader.LoadOneAsync(_db, action.Id, cancellationToken);
    }
}

internal static class ActionPolicyStore
{
    public static async Task<AutomationPolicy> RequireAsync(IAppDbContext db, Guid tenantId, CancellationToken cancellationToken)
    {
        var policy = await db.AutomationPolicies.FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);
        if (policy is not null)
        {
            return policy;
        }

        policy = AutomationPolicy.CreateDefault(tenantId);
        db.AutomationPolicies.Add(policy);
        await db.SaveChangesAsync(cancellationToken);
        return policy;
    }

    public static async Task<SubscriptionPlan> CurrentPlan(IAppDbContext db, Guid tenantId, CancellationToken cancellationToken)
    {
        var subscription = await db.Subscriptions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken)
            ?? throw AppException.Validation("Complete plan selection before opening the action center.");
        return await db.Plans.AsNoTracking().FirstAsync(p => p.Id == subscription.PlanId, cancellationToken);
    }

    public static Task<int> UsedThisMonthAsync(IAppDbContext db, Guid tenantId, CancellationToken cancellationToken)
    {
        var start = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        return db.WorkActions.CountAsync(a => a.TenantId == tenantId && a.CreatedAtUtc >= start, cancellationToken);
    }
}

internal static class ActionWorkspaceLoader
{
    public static async Task<ActionWorkspaceResponse> LoadAsync(
        IAppDbContext db,
        AutomationPolicy policy,
        SubscriptionPlan plan,
        int used,
        Guid businessId,
        CancellationToken cancellationToken)
    {
        var actions = await db.WorkActions.AsNoTracking()
            .Where(a => a.BusinessId == businessId)
            .OrderByDescending(a => a.UpdatedAtUtc)
            .Take(40)
            .ToListAsync(cancellationToken);
        var ids = actions.Select(a => a.Id).ToList();
        var attempts = await db.ActionAttempts.AsNoTracking()
            .Where(a => ids.Contains(a.WorkActionId))
            .OrderBy(a => a.Ordinal)
            .ToListAsync(cancellationToken);
        var verifications = await db.ActionVerifications.AsNoTracking()
            .Where(v => ids.Contains(v.WorkActionId))
            .ToListAsync(cancellationToken);

        return new ActionWorkspaceResponse(
            policy.ToResponse(),
            plan.ActionsPerMonth,
            used,
            "The action engine records approval, attempts, and verification. Live writes stay held unless an official adapter can perform them. Full Auto never invents a provider result.",
            ActionKindCatalog.All.Select(k => k.ToResponse()).ToList(),
            actions.Select(a => a.ToResponse(
                attempts.Where(x => x.WorkActionId == a.Id).ToList(),
                verifications.Where(v => v.WorkActionId == a.Id).OrderByDescending(v => v.CreatedAtUtc).FirstOrDefault())).ToList());
    }

    public static async Task<WorkActionResponse> LoadOneAsync(IAppDbContext db, Guid actionId, CancellationToken cancellationToken)
    {
        var action = await db.WorkActions.AsNoTracking().FirstAsync(a => a.Id == actionId, cancellationToken);
        var attempts = await db.ActionAttempts.AsNoTracking()
            .Where(a => a.WorkActionId == actionId)
            .OrderBy(a => a.Ordinal)
            .ToListAsync(cancellationToken);
        var verification = await db.ActionVerifications.AsNoTracking()
            .Where(v => v.WorkActionId == actionId)
            .OrderByDescending(v => v.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return action.ToResponse(attempts, verification);
    }
}
