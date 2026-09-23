namespace DigitalPulse.Contracts.Connections;

public sealed record PlatformCatalogItem(
    string Code,
    string Name,
    string Category,
    string AuthMode,
    string Summary,
    PlatformCapabilityResponse Capabilities);

public sealed record PlatformCapabilityResponse(
    bool CanRead,
    bool CanCreate,
    bool CanUpdate,
    bool CanDelete,
    bool CanPublish,
    bool CanGetMetrics,
    bool AssistedOnly);

public sealed record ConnectionResponse(
    Guid Id,
    Guid BusinessId,
    string PlatformCode,
    string PlatformName,
    string Category,
    string Status,
    string AuthMode,
    string? ExternalAccount,
    string? GrantKind,
    DateTimeOffset? ConnectedAtUtc,
    DateTimeOffset? LastHealthAtUtc,
    string? LastHealthStatus,
    string? LastError,
    PlatformCapabilityResponse Capabilities);

public sealed record ConnectionCenterResponse(
    IReadOnlyList<PlatformCatalogItem> Catalog,
    IReadOnlyList<ConnectionResponse> Connections,
    int MaxConnections);

public sealed record StartConnectionRequest(string PlatformCode);

public sealed record StartConnectionResponse(
    ConnectionResponse Connection,
    string? AuthorizationUrl,
    bool CompleteInPlace);

public sealed record CompleteConnectionRequest(string? Code);

public sealed record DiagnosticResponse(string Check, string Status, string Detail);
