using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Platforms;

public sealed class PlatformConnection : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public string PlatformCode { get; private set; } = string.Empty;
    public ConnectionStatus Status { get; private set; }
    public PlatformAuthMode AuthMode { get; private set; }
    public string? ExternalAccount { get; private set; }
    public string? GrantKind { get; private set; }
    public string? GrantReference { get; private set; }
    public string? AuthorizationState { get; private set; }
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTimeOffset? TokenExpiresAtUtc { get; private set; }
    public string? TokenScope { get; private set; }
    public DateTimeOffset? ConnectedAtUtc { get; private set; }
    public DateTimeOffset? LastHealthAtUtc { get; private set; }
    public string? LastHealthStatus { get; private set; }
    public string? LastError { get; private set; }

    private PlatformConnection() { }

    public static PlatformConnection Start(
        Guid tenantId,
        Guid businessId,
        string platformCode,
        PlatformAuthMode authMode,
        string authorizationState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizationState);
        return new PlatformConnection
        {
            TenantId = tenantId,
            BusinessId = businessId,
            PlatformCode = platformCode.Trim().ToUpperInvariant(),
            AuthMode = authMode,
            Status = ConnectionStatus.Connecting,
            AuthorizationState = authorizationState
        };
    }

    public bool HasLiveCredential =>
        Status == ConnectionStatus.Connected
        && !string.IsNullOrWhiteSpace(AccessToken)
        && GrantKind is "OAuth" or "ApiKey";

    public void MarkConnected(string grantKind, string grantReference, string? externalAccount)
    {
        Status = ConnectionStatus.Connected;
        GrantKind = grantKind;
        GrantReference = grantReference;
        ExternalAccount = externalAccount;
        AuthorizationState = null;
        ConnectedAtUtc = DateTimeOffset.UtcNow;
        LastError = null;
        LastHealthStatus = "Healthy";
        LastHealthAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void AttachLiveGrant(
        string grantKind,
        string grantReference,
        string? externalAccount,
        string accessToken,
        string? refreshToken,
        DateTimeOffset? expiresAtUtc,
        string? scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        MarkConnected(grantKind, grantReference, externalAccount);
        AccessToken = accessToken.Trim();
        RefreshToken = string.IsNullOrWhiteSpace(refreshToken) ? null : refreshToken.Trim();
        TokenExpiresAtUtc = expiresAtUtc;
        TokenScope = string.IsNullOrWhiteSpace(scope) ? null : scope.Trim();
    }

    public void AssignExternalAccount(string account)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(account);
        ExternalAccount = account.Trim();
        Touch();
    }

    public void ApplyRefreshedTokens(string accessToken, string? refreshToken, DateTimeOffset? expiresAtUtc, string? scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        AccessToken = accessToken.Trim();
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            RefreshToken = refreshToken.Trim();
        }

        TokenExpiresAtUtc = expiresAtUtc;
        if (!string.IsNullOrWhiteSpace(scope))
        {
            TokenScope = scope.Trim();
        }

        Touch();
    }

    public void BeginReauthorize(string authorizationState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizationState);
        Status = ConnectionStatus.Connecting;
        AuthorizationState = authorizationState;
        LastError = null;
        Touch();
    }

    public void MarkNeedsReauth(string reason)
    {
        Status = ConnectionStatus.NeedsReauth;
        LastError = reason;
        LastHealthStatus = "NeedsReauth";
        LastHealthAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void MarkError(string reason)
    {
        Status = ConnectionStatus.Error;
        LastError = reason;
        LastHealthStatus = "Error";
        LastHealthAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void RecordHealth(string status, string? detail)
    {
        LastHealthStatus = status;
        LastHealthAtUtc = DateTimeOffset.UtcNow;
        LastError = status == "Healthy" ? null : detail;
        Touch();
    }
}
