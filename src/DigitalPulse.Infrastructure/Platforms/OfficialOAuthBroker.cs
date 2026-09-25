using System.Net.Http.Headers;
using System.Text.Json;
using DigitalPulse.Application.Abstractions;
using DigitalPulse.Domain.Platforms;
using Microsoft.Extensions.Configuration;

namespace DigitalPulse.Infrastructure.Platforms;

public sealed record OAuthApp(string ClientId, string ClientSecret, string AuthorizeUrl, string TokenUrl, string Scopes);

public static class OfficialOAuthCatalog
{
    public static OAuthApp? TryGet(string platformCode, IConfiguration configuration)
    {
        var code = platformCode.Trim().ToUpperInvariant();
        return code switch
        {
            "GOOGLE" or "SEARCH_CONSOLE" or "YOUTUBE" or "GOOGLE_ADS" or "GOOGLE_ANALYTICS" => Google(code, configuration),
            "FACEBOOK" or "INSTAGRAM" => Pair(
                configuration["Connections:Meta:ClientId"] ?? configuration["Connections:Facebook:ClientId"],
                configuration["Connections:Meta:ClientSecret"] ?? configuration["Connections:Facebook:ClientSecret"],
                "https://www.facebook.com/v21.0/dialog/oauth",
                "https://graph.facebook.com/v21.0/oauth/access_token",
                "pages_show_list,pages_read_engagement,pages_manage_posts,instagram_basic,instagram_content_publish,public_profile"),
            "LINKEDIN" => Pair(
                configuration["Connections:LinkedIn:ClientId"],
                configuration["Connections:LinkedIn:ClientSecret"],
                "https://www.linkedin.com/oauth/v2/authorization",
                "https://www.linkedin.com/oauth/v2/accessToken",
                "openid profile w_member_social"),
            _ => null
        };
    }

    public static string? ApiKey(string platformCode, IConfiguration configuration) =>
        platformCode.Trim().ToUpperInvariant() switch
        {
            "INDIAMART" => configuration["Connections:IndiaMART:CrmKey"],
            "WHATSAPP" => configuration["WhatsApp:CloudApi:AccessToken"],
            _ => null
        };

    private static OAuthApp? Google(string platform, IConfiguration configuration)
    {
        var scopes = platform switch
        {
            "SEARCH_CONSOLE" => "openid email https://www.googleapis.com/auth/webmasters",
            "YOUTUBE" => "openid email https://www.googleapis.com/auth/youtube.readonly https://www.googleapis.com/auth/youtube.upload",
            "GOOGLE_ADS" => "openid email https://www.googleapis.com/auth/adwords",
            "GOOGLE_ANALYTICS" => "openid email https://www.googleapis.com/auth/analytics.readonly",
            _ => "openid email https://www.googleapis.com/auth/business.manage"
        };
        return Pair(
            configuration["Connections:Google:ClientId"],
            configuration["Connections:Google:ClientSecret"],
            "https://accounts.google.com/o/oauth2/v2/auth",
            "https://oauth2.googleapis.com/token",
            scopes);
    }

    private static OAuthApp? Pair(string? id, string? secret, string authorize, string token, string scopes) =>
        string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(secret)
            ? null
            : new OAuthApp(id.Trim(), secret.Trim(), authorize, token, scopes);
}

public sealed class OfficialOAuthBroker : IPlatformAuthorizationBroker, ILiveTokenRefresher
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _configuration;

    public OfficialOAuthBroker(IHttpClientFactory http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    public Task<AuthorizationStart> StartAsync(PlatformConnection connection, IPlatformAdapter adapter, CancellationToken cancellationToken)
    {
        var app = OfficialOAuthCatalog.TryGet(connection.PlatformCode, _configuration);
        if (app is not null)
        {
            var redirect = RedirectUri();
            var url =
                $"{app.AuthorizeUrl}?response_type=code&client_id={Uri.EscapeDataString(app.ClientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirect)}" +
                $"&scope={Uri.EscapeDataString(app.Scopes)}" +
                $"&state={Uri.EscapeDataString(connection.AuthorizationState ?? string.Empty)}" +
                "&access_type=offline&prompt=consent";
            return Task.FromResult(new AuthorizationStart(url, false));
        }

        if (!string.IsNullOrWhiteSpace(OfficialOAuthCatalog.ApiKey(connection.PlatformCode, _configuration)) ||
            adapter.Describe().AuthMode == PlatformAuthMode.Assisted)
        {
            return Task.FromResult(new AuthorizationStart(string.Empty, true));
        }

        var fallback = $"/v1/connections/callback?state={Uri.EscapeDataString(connection.AuthorizationState!)}&code=development";
        return Task.FromResult(new AuthorizationStart(fallback, false));
    }

    public async Task CompleteAsync(PlatformConnection connection, string? code, CancellationToken cancellationToken)
    {
        var app = OfficialOAuthCatalog.TryGet(connection.PlatformCode, _configuration);
        if (app is not null && !string.Equals(code, "development", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                connection.MarkError("The official provider did not return an authorization code.");
                return;
            }

            await ExchangeAsync(connection, app, code, cancellationToken);
            return;
        }

        var apiKey = OfficialOAuthCatalog.ApiKey(connection.PlatformCode, _configuration);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            connection.AttachLiveGrant("ApiKey", connection.PlatformCode.ToLowerInvariant(), connection.PlatformCode, apiKey, null, null, connection.PlatformCode);
            return;
        }

        if (connection.AuthMode == PlatformAuthMode.Assisted)
        {
            connection.MarkConnected("Assisted", "assisted", "Assisted workspace");
            return;
        }

        if (connection.AuthMode != PlatformAuthMode.Assisted &&
            !string.Equals(code, "development", StringComparison.OrdinalIgnoreCase))
        {
            connection.MarkError("Authorization code was not a development grant, and official OAuth is not configured.");
            return;
        }

        connection.MarkConnected("Development", Guid.NewGuid().ToString("N")[..16], $"Development · {connection.PlatformCode}");
    }

    public async Task EnsureFreshAsync(PlatformConnection connection, CancellationToken cancellationToken)
    {
        if (!connection.HasLiveCredential ||
            !string.Equals(connection.GrantKind, "OAuth", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (connection.TokenExpiresAtUtc is null)
        {
            return;
        }

        if (connection.TokenExpiresAtUtc > DateTimeOffset.UtcNow.Add(RefreshSkew))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(connection.RefreshToken))
        {
            connection.MarkNeedsReauth("The official access token expired and no refresh token is stored. Reauthorize.");
            return;
        }

        var app = OfficialOAuthCatalog.TryGet(connection.PlatformCode, _configuration);
        if (app is null)
        {
            connection.MarkNeedsReauth("Official OAuth is not configured on this host. The stored token cannot be refreshed.");
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, app.TokenUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = connection.RefreshToken,
                ["client_id"] = app.ClientId,
                ["client_secret"] = app.ClientSecret
            })
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await _http.CreateClient("official-platforms").SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            connection.MarkNeedsReauth($"Official token refresh failed ({(int)response.StatusCode}). DigitalPulse did not invent a grant.");
            return;
        }

        if (!TryReadAccessToken(body, app.Scopes, out var access, out var refresh, out var expires, out var scope) ||
            string.IsNullOrWhiteSpace(access))
        {
            connection.MarkNeedsReauth("The official refresh response did not include an access token.");
            return;
        }

        connection.ApplyRefreshedTokens(access, refresh, expires, scope);
    }

    private async Task ExchangeAsync(PlatformConnection connection, OAuthApp app, string code, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, app.TokenUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["client_id"] = app.ClientId,
                ["client_secret"] = app.ClientSecret,
                ["redirect_uri"] = RedirectUri()
            })
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await _http.CreateClient("official-platforms").SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            connection.MarkError($"Official token exchange failed ({(int)response.StatusCode}). DigitalPulse did not invent a grant.");
            return;
        }

        if (!TryReadAccessToken(body, app.Scopes, out var access, out var refresh, out var expires, out var scope) ||
            string.IsNullOrWhiteSpace(access))
        {
            connection.MarkError("The official token response did not include an access token.");
            return;
        }

        var account = await TryIdentityAsync(connection.PlatformCode, access, cancellationToken)
            ?? connection.PlatformCode;
        connection.AttachLiveGrant(
            "OAuth",
            $"oauth-{connection.PlatformCode.ToLowerInvariant()}",
            account,
            access,
            refresh,
            expires,
            scope);
    }

    private async Task<string?> TryIdentityAsync(string platformCode, string accessToken, CancellationToken cancellationToken)
    {
        var url = platformCode.Trim().ToUpperInvariant() switch
        {
            "FACEBOOK" or "INSTAGRAM" => "https://graph.facebook.com/v21.0/me?fields=id,name",
            "LINKEDIN" => "https://api.linkedin.com/v2/userinfo",
            _ => "https://www.googleapis.com/oauth2/v3/userinfo"
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await _http.CreateClient("official-platforms").SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = doc.RootElement;
            return root.TryGetProperty("sub", out var sub) ? sub.GetString()
                : root.TryGetProperty("id", out var id) ? id.GetString()
                : root.TryGetProperty("email", out var email) ? email.GetString()
                : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return null;
        }
    }

    private static bool TryReadAccessToken(
        string body,
        string fallbackScope,
        out string? access,
        out string? refresh,
        out DateTimeOffset? expires,
        out string? scope)
    {
        access = null;
        refresh = null;
        expires = null;
        scope = fallbackScope;
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            var root = doc.RootElement;
            access = root.TryGetProperty("access_token", out var at) ? at.GetString() : null;
            refresh = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
            if (root.TryGetProperty("scope", out var sc))
            {
                scope = sc.GetString() ?? fallbackScope;
            }

            if (root.TryGetProperty("expires_in", out var exp) && exp.TryGetInt32(out var seconds))
            {
                expires = DateTimeOffset.UtcNow.AddSeconds(seconds);
            }

            return !string.IsNullOrWhiteSpace(access);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private string RedirectUri() =>
        _configuration["Connections:RedirectUri"] ?? "http://localhost:5088/v1/connections/callback";
}
