using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Platforms;

public sealed class HostOAuthApp : Entity
{
    public string Provider { get; private set; } = string.Empty;
    public string ClientId { get; private set; } = string.Empty;
    public string ClientSecret { get; private set; } = string.Empty;

    private HostOAuthApp() { }

    public static HostOAuthApp Create(string provider, string clientId, string clientSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        return new HostOAuthApp
        {
            Provider = Normalize(provider),
            ClientId = clientId.Trim(),
            ClientSecret = clientSecret.Trim()
        };
    }

    public void Update(string clientId, string? clientSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ClientId = clientId.Trim();
        if (!string.IsNullOrWhiteSpace(clientSecret))
        {
            ClientSecret = clientSecret.Trim();
        }

        Touch();
    }

    public static string Normalize(string provider) => provider.Trim().ToUpperInvariant() switch
    {
        "GOOGLE" or "YOUTUBE" or "SEARCH_CONSOLE" or "GOOGLE_ADS" or "GOOGLE_ANALYTICS" => "GOOGLE",
        "FACEBOOK" or "INSTAGRAM" or "META" => "META",
        "LINKEDIN" => "LINKEDIN",
        var value => value
    };
}
