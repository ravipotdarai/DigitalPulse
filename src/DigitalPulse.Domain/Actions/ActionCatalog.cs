using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Actions;

public enum ActionKind
{
    AnalyzeWebsite = 0,
    RunScan = 1,
    RebuildGraphify = 2,
    RunAi = 3,
    RefreshSocialMetrics = 4,
    MonitorDirectory = 5,
    PublishSocial = 6,
    VerifyDirectory = 7,
    SendWhatsAppTemplate = 8,
    SendWhatsAppSession = 9,
    RunMonitoring = 10,
    AssembleReport = 11,
    AssembleAgencyReport = 12,
    StartAgencyWorkflow = 13
}

public enum ActionRisk
{
    Low = 0,
    Medium = 1,
    High = 2
}

public enum ActionStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Queued = 3,
    Executing = 4,
    Executed = 5,
    Verified = 6,
    Failed = 7,
    Assisted = 8,
    Escalated = 9
}

public enum AutomationMode
{
    Assisted = 0,
    FullAuto = 1
}

public enum VerificationStatus
{
    Pending = 0,
    Held = 1,
    Verified = 2,
    Failed = 3
}

public sealed record ActionKindDescriptor(ActionKind Kind, string Code, string Name, ActionRisk Risk, bool ExternalWrite, string Purpose);

public static class ActionKindCatalog
{
    public static readonly IReadOnlyList<ActionKindDescriptor> All =
    [
        new(ActionKind.AnalyzeWebsite, "analyze-website", "Analyze website", ActionRisk.Low, false, "Safe homepage snapshot. Not a live Search Console write."),
        new(ActionKind.RunScan, "run-scan", "Run DigitalPulse Check", ActionRisk.Low, false, "Compares identity to stored observations."),
        new(ActionKind.RebuildGraphify, "rebuild-graphify", "Rebuild Graphify", ActionRisk.Low, false, "Rebuilds the business context graph from the record."),
        new(ActionKind.RunAi, "run-ai", "Run AI orchestrator", ActionRisk.Medium, false, "Evidence-backed composition. Never publishes."),
        new(ActionKind.RefreshSocialMetrics, "refresh-social-metrics", "Refresh social metrics", ActionRisk.Medium, false, "Asks the adapter for metrics. Development grants stay unavailable."),
        new(ActionKind.MonitorDirectory, "monitor-directory", "Monitor directory", ActionRisk.Low, false, "Assisted directory watch. Official reads are not invented."),
        new(ActionKind.PublishSocial, "publish-social", "Publish social", ActionRisk.High, true, "External write. Holds unless a live publish adapter exists."),
        new(ActionKind.VerifyDirectory, "verify-directory", "Verify directory", ActionRisk.High, true, "Operator-confirmed directory verification. Not an unofficial write."),
        new(ActionKind.SendWhatsAppTemplate, "send-whatsapp-template", "Send WhatsApp template", ActionRisk.High, true, "Cloud API template send. Opt-in and approval required. Unofficial clients are out of scope."),
        new(ActionKind.SendWhatsAppSession, "send-whatsapp-session", "Send WhatsApp session reply", ActionRisk.High, true, "Cloud API session reply inside the 24-hour window. Never an unofficial send."),
        new(ActionKind.RunMonitoring, "run-monitoring", "Run monitoring", ActionRisk.Low, false, "Records stored health and honest holds. Live provider metrics are not invented."),
        new(ActionKind.AssembleReport, "assemble-report", "Assemble presence report", ActionRisk.Low, false, "Assembles Observed Fact, Recommendation, AI Interpretation, and Customer Decision from stored observations."),
        new(ActionKind.AssembleAgencyReport, "assemble-agency-report", "Assemble agency report", ActionRisk.Low, false, "Assembles a client or portfolio report from stored work. Live provider metrics are not invented."),
        new(ActionKind.StartAgencyWorkflow, "start-agency-workflow", "Start agency workflow", ActionRisk.Low, false, "Starts onboarding, review, audit, or white-label review. Custom-domain hosting stays held.")
    ];

    public static ActionKindDescriptor Require(string code) =>
        All.FirstOrDefault(a => a.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException("Unknown action kind.");

    public static ActionKindDescriptor Of(ActionKind kind) => All.First(a => a.Kind == kind);
}

public sealed record ActionPolicyDecision(
    bool Allowed,
    bool RequiresApproval,
    bool EligibleForAutopilot,
    ActionRisk Risk,
    string Reason);

public static class ActionPolicy
{
    public static ActionPolicyDecision Evaluate(
        ActionKind kind,
        AutomationMode mode,
        bool liveWriteAvailable,
        bool alreadyApproved)
    {
        var descriptor = ActionKindCatalog.Of(kind);
        if (descriptor.ExternalWrite && !liveWriteAvailable)
        {
            return new ActionPolicyDecision(
                true,
                true,
                false,
                descriptor.Risk,
                "No live write adapter. DigitalPulse will not invent an external publish. The action stays assisted.");
        }

        if (descriptor.Risk == ActionRisk.High && !alreadyApproved)
        {
            return new ActionPolicyDecision(
                true,
                true,
                false,
                descriptor.Risk,
                "High-risk change requires explicit approval.");
        }

        if (mode == AutomationMode.FullAuto &&
            descriptor.Risk == ActionRisk.Low &&
            !descriptor.ExternalWrite)
        {
            return new ActionPolicyDecision(
                true,
                false,
                true,
                descriptor.Risk,
                "Low-risk, policy-approved, and not an external write. Eligible for autopilot.");
        }

        return new ActionPolicyDecision(
            true,
            !alreadyApproved,
            false,
            descriptor.Risk,
            "Assisted mode. A human must approve or execute. Autopilot does not invent live provider work.");
    }
}

public sealed class AutomationPolicy : TenantOwnedEntity
{
    public AutomationMode Mode { get; private set; }
    public bool AllowLowRiskAuto { get; private set; } = true;
    public bool RequireApprovalForHighRisk { get; private set; } = true;
    public int MaxAttempts { get; private set; } = 3;

    private AutomationPolicy() { }

    public static AutomationPolicy CreateDefault(Guid tenantId) =>
        new()
        {
            TenantId = tenantId,
            Mode = AutomationMode.Assisted,
            AllowLowRiskAuto = true,
            RequireApprovalForHighRisk = true,
            MaxAttempts = 3
        };

    public void Update(AutomationMode mode, bool allowLowRiskAuto, int maxAttempts)
    {
        if (maxAttempts is < 1 or > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be between 1 and 8.");
        }

        Mode = mode;
        AllowLowRiskAuto = allowLowRiskAuto;
        RequireApprovalForHighRisk = true;
        MaxAttempts = maxAttempts;
        Touch();
    }
}

public sealed class WorkAction : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public ActionKind Kind { get; private set; }
    public ActionStatus Status { get; private set; }
    public ActionRisk Risk { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public Guid? TargetId { get; private set; }
    public string? TargetLabel { get; private set; }
    public bool LiveWriteAvailable { get; private set; }
    public bool AutopilotEligible { get; private set; }
    public string HoldReason { get; private set; } = string.Empty;
    public int AttemptCount { get; private set; }
    public DateTimeOffset? NextRetryAtUtc { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public DateTimeOffset? ExecutedAtUtc { get; private set; }

    private WorkAction() { }

    public static WorkAction Enqueue(
        Guid tenantId,
        Guid businessId,
        ActionKindDescriptor descriptor,
        string title,
        string idempotencyKey,
        Guid? targetId,
        string? targetLabel,
        bool liveWriteAvailable,
        ActionPolicyDecision decision)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        var action = new WorkAction
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Kind = descriptor.Kind,
            Risk = descriptor.Risk,
            Title = title.Trim(),
            IdempotencyKey = idempotencyKey.Trim(),
            TargetId = targetId,
            TargetLabel = string.IsNullOrWhiteSpace(targetLabel) ? null : targetLabel.Trim(),
            LiveWriteAvailable = liveWriteAvailable,
            AutopilotEligible = decision.EligibleForAutopilot,
            HoldReason = decision.Reason,
            Status = decision.EligibleForAutopilot
                ? ActionStatus.Queued
                : decision.RequiresApproval
                    ? ActionStatus.PendingApproval
                    : ActionStatus.Approved
        };
        return action;
    }

    public void Approve()
    {
        if (Status is not (ActionStatus.Draft or ActionStatus.PendingApproval or ActionStatus.Assisted))
        {
            throw new InvalidOperationException("Only pending or assisted actions can be approved.");
        }

        Status = AutopilotEligible ? ActionStatus.Queued : ActionStatus.Approved;
        ApprovedAtUtc = DateTimeOffset.UtcNow;
        HoldReason = AutopilotEligible
            ? "Approved and queued for autopilot."
            : "Approved. Execution stays assisted until a live adapter exists.";
        Touch();
    }

    public void MarkExecuting()
    {
        if (Status is not (ActionStatus.Approved or ActionStatus.Queued or ActionStatus.Failed))
        {
            throw new InvalidOperationException("Approve or queue the action before executing.");
        }

        Status = ActionStatus.Executing;
        Touch();
    }

    public void MarkExecuted(string detail, bool held)
    {
        Status = held ? ActionStatus.Assisted : ActionStatus.Executed;
        HoldReason = detail;
        ExecutedAtUtc = DateTimeOffset.UtcNow;
        NextRetryAtUtc = null;
        Touch();
    }

    public void MarkFailed(string detail, int maxAttempts)
    {
        AttemptCount += 1;
        Status = AttemptCount >= maxAttempts ? ActionStatus.Escalated : ActionStatus.Failed;
        HoldReason = detail;
        NextRetryAtUtc = Status == ActionStatus.Failed
            ? DateTimeOffset.UtcNow.AddMinutes(Math.Pow(2, AttemptCount))
            : null;
        Touch();
    }

    public void MarkVerified(string detail)
    {
        if (Status is not (ActionStatus.Executed or ActionStatus.Assisted))
        {
            throw new InvalidOperationException("Verify only after execution.");
        }

        Status = ActionStatus.Verified;
        HoldReason = detail;
        Touch();
    }

    public bool CanRetry(int maxAttempts) =>
        Status == ActionStatus.Failed && AttemptCount < maxAttempts;
}

public sealed class ActionAttempt : TenantOwnedEntity
{
    public Guid WorkActionId { get; private set; }
    public int Ordinal { get; private set; }
    public string Outcome { get; private set; } = string.Empty;
    public string Detail { get; private set; } = string.Empty;

    private ActionAttempt() { }

    public static ActionAttempt Record(Guid tenantId, Guid actionId, int ordinal, string outcome, string detail) =>
        new()
        {
            TenantId = tenantId,
            WorkActionId = actionId,
            Ordinal = ordinal,
            Outcome = outcome.Trim(),
            Detail = detail.Trim()
        };
}

public sealed class ActionVerification : TenantOwnedEntity
{
    public Guid WorkActionId { get; private set; }
    public VerificationStatus Status { get; private set; }
    public string Detail { get; private set; } = string.Empty;

    private ActionVerification() { }

    public static ActionVerification Record(Guid tenantId, Guid actionId, VerificationStatus status, string detail) =>
        new()
        {
            TenantId = tenantId,
            WorkActionId = actionId,
            Status = status,
            Detail = detail.Trim()
        };
}
