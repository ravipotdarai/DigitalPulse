namespace DigitalPulse.Contracts.Monitoring;

public sealed record MonitoringKindResponse(string Code, string Name, bool CanObserveWithoutLiveApi, string Purpose);

public sealed record MonitoringScheduleResponse(
    int IntervalHours,
    bool Enabled,
    DateTimeOffset? LastRunAtUtc,
    DateTimeOffset? NextRunAtUtc,
    bool Due,
    string HoldReason);

public sealed record MonitoringResultResponse(
    Guid Id,
    string Kind,
    string Status,
    string Title,
    string ObservedFact,
    string Recommendation,
    string? PreviousValue,
    string? CurrentValue);

public sealed record MonitoringRunResponse(
    Guid Id,
    string Trigger,
    string Status,
    string Summary,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    IReadOnlyList<MonitoringResultResponse> Results);

public sealed record MonitoringAlertResponse(
    Guid Id,
    string Severity,
    string Status,
    string Title,
    string Detail,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset? AcknowledgedAtUtc);

public sealed record CompetitorResponse(Guid Id, string Name, string? Website, string? Notes);

public sealed record PresenceReportResponse(
    Guid Id,
    string Kind,
    string Title,
    string ObservedFact,
    string Recommendation,
    string AiInterpretation,
    string? CustomerDecision,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    string HoldReason);

public sealed record MonitoringWorkspaceResponse(
    MonitoringScheduleResponse Schedule,
    string Note,
    IReadOnlyList<MonitoringKindResponse> Kinds,
    IReadOnlyList<MonitoringRunResponse> Runs,
    IReadOnlyList<MonitoringAlertResponse> Alerts,
    IReadOnlyList<CompetitorResponse> Competitors,
    IReadOnlyList<PresenceReportResponse> Reports);

public sealed record AddCompetitorRequest(string Name, string? Website, string? Notes);
public sealed record RecordReportDecisionRequest(string Decision);
