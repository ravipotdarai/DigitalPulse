namespace DigitalPulse.Contracts.Directories;

public sealed record DirectoryCapabilityResponse(
    string PlatformCode,
    string PlatformName,
    bool CanRead,
    bool CanWriteOfficially,
    bool AssistedOnly,
    string OfficialRead,
    string OfficialWrite);

public sealed record DirectoryProviderResponse(
    DirectoryCapabilityResponse Capabilities,
    string? ConnectionStatus,
    string? GrantKind,
    string? LastHealthStatus,
    string ReadStatus,
    string ReadDetail);

public sealed record DirectoryStepResponse(
    Guid Id,
    int Ordinal,
    string Title,
    string Detail,
    DateTimeOffset? CompletedAtUtc);

public sealed record DirectoryTaskResponse(
    Guid Id,
    string PlatformCode,
    string Kind,
    string Status,
    string PreparedName,
    string? PreparedPhone,
    string? PreparedWebsite,
    string? PreparedCategory,
    string? PreparedServices,
    string? VerificationNote,
    DateTimeOffset? VerifiedAtUtc,
    DateTimeOffset? LastMonitoredAtUtc,
    string? MonitorDetail,
    IReadOnlyList<DirectoryStepResponse> Steps);

public sealed record DirectoryWorkspaceResponse(
    IReadOnlyList<DirectoryProviderResponse> Providers,
    IReadOnlyList<DirectoryTaskResponse> Tasks,
    string Note);

public sealed record PrepareDirectoryRequest(string PlatformCode);
public sealed record VerifyDirectoryRequest(string Note);
