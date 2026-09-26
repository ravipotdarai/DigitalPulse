namespace DigitalPulse.Contracts.Connections;

public sealed record PlatformCatalogItem(
    string Code,
    string Name,
    string Category,
    string AuthMode,
    string Summary,
    PlatformCapabilityResponse Capabilities,
    bool OfficialLoginReady);

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
    bool HasLiveCredential,
    DateTimeOffset? ConnectedAtUtc,
    DateTimeOffset? LastHealthAtUtc,
    string? LastHealthStatus,
    string? LastError,
    PlatformCapabilityResponse Capabilities);

public sealed record ConnectionCenterResponse(
    IReadOnlyList<PlatformCatalogItem> Catalog,
    IReadOnlyList<ConnectionResponse> Connections,
    int MaxConnections,
    OfficialOAuthAppsStatus OfficialApps);

public sealed record StartConnectionRequest(string PlatformCode);

public sealed record StartConnectionResponse(
    ConnectionResponse Connection,
    string? AuthorizationUrl,
    bool CompleteInPlace,
    bool NeedsOfficialApp);

public sealed record CompleteConnectionRequest(string? Code);

public sealed record DiagnosticResponse(string Check, string Status, string Detail);

public sealed record ConnectionAccountOption(string Id, string Label, string Kind);

public sealed record SelectConnectionAccountRequest(string ExternalAccount);

public sealed record OfficialOAuthAppState(string Provider, bool Ready, string? ClientIdMasked);

public sealed record OfficialOAuthAppsStatus(string RedirectUri, IReadOnlyList<OfficialOAuthAppState> Apps);

public sealed record SaveOfficialOAuthAppsRequest(
    string? GoogleClientId,
    string? GoogleClientSecret,
    string? MetaClientId,
    string? MetaClientSecret,
    string? LinkedInClientId,
    string? LinkedInClientSecret);
