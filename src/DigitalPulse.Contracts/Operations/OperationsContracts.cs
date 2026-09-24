namespace DigitalPulse.Contracts.Operations;

public sealed record StartDisasterDrillRequest(string Kind);
public sealed record RunInventoryRequest(string? Kind = null);

public sealed record BackupSnapshotResponse(
    Guid Id,
    string Status,
    string Manifest,
    string Checksum,
    string HoldReason,
    DateTimeOffset CreatedAtUtc);

public sealed record RestoreAttemptResponse(
    Guid Id,
    Guid SnapshotId,
    string Status,
    string HoldReason,
    DateTimeOffset CreatedAtUtc);

public sealed record DisasterDrillResponse(
    Guid Id,
    string Kind,
    string Status,
    string ObservedFact,
    string HoldReason,
    DateTimeOffset CreatedAtUtc);

public sealed record DependencyInventoryResponse(
    Guid Id,
    string Kind,
    string Status,
    int PackageCount,
    string Packages,
    string HoldReason,
    DateTimeOffset CreatedAtUtc);

public sealed record ReadinessCheckResponse(string Code, string Title, string Outcome, string Detail);

public sealed record ReadinessReviewResponse(
    Guid Id,
    string Status,
    string EnvironmentName,
    int HoldCount,
    int FailCount,
    string HoldReason,
    IReadOnlyList<ReadinessCheckResponse> Checks);

public sealed record CostControlResponse(string Meter, int Used, int Included, string Note);

public sealed record OperationsAuditResponse(Guid Id, string Action, string Detail, DateTimeOffset CreatedAtUtc);

public sealed record OperationsWorkspaceResponse(
    string EnvironmentName,
    string HostRole,
    bool RateLimitingEnabled,
    int RateLimitPerMinute,
    bool KeyVaultConfigured,
    bool AppInsightsConfigured,
    bool RedisConfigured,
    bool AzureBackupConfigured,
    string Note,
    IReadOnlyList<CostControlResponse> Cost,
    IReadOnlyList<BackupSnapshotResponse> Backups,
    IReadOnlyList<RestoreAttemptResponse> Restores,
    IReadOnlyList<DisasterDrillResponse> Drills,
    IReadOnlyList<DependencyInventoryResponse> Inventories,
    ReadinessReviewResponse? Readiness,
    IReadOnlyList<OperationsAuditResponse> Audits);
