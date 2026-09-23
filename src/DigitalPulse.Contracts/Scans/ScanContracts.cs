namespace DigitalPulse.Contracts.Scans;

public sealed record ScanSummaryResponse(
    Guid Id,
    Guid BusinessId,
    string Trigger,
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? Summary,
    string? Error,
    int FindingCount,
    int OpenCount,
    int CriticalCount);

public sealed record EvidenceResponse(
    Guid Id,
    string Kind,
    string Label,
    string Value,
    string Source);

public sealed record FindingResponse(
    Guid Id,
    Guid ScanId,
    Guid BusinessId,
    string Category,
    string Severity,
    string Title,
    string Description,
    string? ExpectedValue,
    string? ObservedValue,
    string Recommendation,
    string SuggestedAction,
    string VerificationMethod,
    string AutomationState,
    string Status,
    IReadOnlyList<EvidenceResponse> Evidence);

public sealed record ScanDetailResponse(
    Guid Id,
    Guid BusinessId,
    string Trigger,
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? Summary,
    string? Error,
    int ScansPerMonth,
    int ScansUsedThisMonth,
    IReadOnlyList<FindingResponse> Findings);

public sealed record ScanCenterResponse(
    IReadOnlyList<ScanSummaryResponse> Scans,
    ScanDetailResponse? Latest,
    int ScansPerMonth,
    int ScansUsedThisMonth);

public sealed record UpdateFindingRequest(string Status);
