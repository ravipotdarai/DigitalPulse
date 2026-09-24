namespace DigitalPulse.Contracts.Actions;

public sealed record ActionKindResponse(string Code, string Name, string Risk, bool ExternalWrite, string Purpose);

public sealed record AutomationPolicyResponse(string Mode, bool AllowLowRiskAuto, bool RequireApprovalForHighRisk, int MaxAttempts);

public sealed record ActionAttemptResponse(int Ordinal, string Outcome, string Detail, DateTimeOffset AtUtc);

public sealed record ActionVerificationResponse(string Status, string Detail, DateTimeOffset AtUtc);

public sealed record WorkActionResponse(
    Guid Id,
    string Kind,
    string Status,
    string Risk,
    string Title,
    string IdempotencyKey,
    Guid? TargetId,
    string? TargetLabel,
    bool LiveWriteAvailable,
    bool AutopilotEligible,
    string HoldReason,
    int AttemptCount,
    DateTimeOffset? NextRetryAtUtc,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<ActionAttemptResponse> Attempts,
    ActionVerificationResponse? Verification);

public sealed record ActionWorkspaceResponse(
    AutomationPolicyResponse Policy,
    int ActionsPerMonth,
    int ActionsUsedThisMonth,
    string Note,
    IReadOnlyList<ActionKindResponse> Kinds,
    IReadOnlyList<WorkActionResponse> Actions);

public sealed record UpdateAutomationPolicyRequest(string Mode, bool AllowLowRiskAuto, int MaxAttempts);

public sealed record EnqueueActionRequest(string Kind, string Title, Guid? TargetId, string? TargetLabel);
