using DigitalPulse.Contracts.Connections;

namespace DigitalPulse.Application.Abstractions;

public sealed record OAuthApp(string ClientId, string ClientSecret, string AuthorizeUrl, string TokenUrl, string Scopes);

public interface IOfficialOAuthApps
{
    OAuthApp? Resolve(string platformCode);
    string? ApiKey(string platformCode);
    OfficialOAuthAppsStatus Status();
    string RedirectUri();
    Task SaveAsync(SaveOfficialOAuthAppsRequest request, CancellationToken cancellationToken);
}
