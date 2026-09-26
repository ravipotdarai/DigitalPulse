using System.Net.Http.Headers;
using System.Text.Json;
using DigitalPulse.Application.Abstractions;
using DigitalPulse.Domain.Platforms;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DigitalPulse.Infrastructure.Platforms;

public static class OfficialOAuthCatalog
{
    public static OAuthApp? TryGet(string platformCode, IConfiguration configuration) =>
        TryGet(
            platformCode,
            configuration["Connections:Google:ClientId"],
            configuration["Connections:Google:ClientSecret"],
            configuration["Connections:Meta:ClientId"] ?? configuration["Connections:Facebook:ClientId"],
            configuration["Connections:Meta:ClientSecret"] ?? configuration["Connections:Facebook:ClientSecret"],
            configuration["Connections:LinkedIn:ClientId"],
            configuration["Connections:LinkedIn:ClientSecret"]);

    public static OAuthApp? TryGet(string platformCode, string? clientId, string? clientSecret)
    {
        var provider = platformCode.Trim().ToUpperInvariant() switch
        {
            "GOOGLE" or "SEARCH_CONSOLE" or "YOUTUBE" or "GOOGLE_ADS" or "GOOGLE_ANALYTICS" => "GOOGLE",
            "FACEBOOK" or "INSTAGRAM" => "META",
            "LINKEDIN" => "LINKEDIN",
            _ => null
        };
        return provider switch
        {
            "GOOGLE" => Google(platformCode.Trim().ToUpperInvariant(), clientId, clientSecret),
            "META" => Pair(
                clientId,
                clientSecret,
                "https://www.facebook.com/v21.0/dialog/oauth",
                "https://graph.facebook.com/v21.0/oauth/access_token",
                "pages_show_list,pages_read_engagement,pages_manage_posts,instagram_basic,instagram_content_publish,public_profile"),
            "LINKEDIN" => Pair(
                clientId,
                clientSecret,
                "https://www.linkedin.com/oauth/v2/authorization",
                "https://www.linkedin.com/oauth/v2/accessToken",
                "openid profile w_member_social"),
            _ => null
        };
    }

    public static OAuthApp? TryGet(
        string platformCode,
        string? googleId,
        string? googleSecret,
        string? metaId,
        string? metaSecret,
        string? linkedInId,
        string? linkedInSecret)
    {
        var code = platformCode.Trim().ToUpperInvariant();
        return code switch
        {
            "GOOGLE" or "SEARCH_CONSOLE" or "YOUTUBE" or "GOOGLE_ADS" or "GOOGLE_ANALYTICS" => Google(code, googleId, googleSecret),
            "FACEBOOK" or "INSTAGRAM" => TryGet(code, metaId, metaSecret),
            "LINKEDIN" => TryGet(code, linkedInId, linkedInSecret),
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

    private static OAuthApp? Google(string platform, string? clientId, string? clientSecret)
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
            clientId,
            clientSecret,
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
    private readonly IHttpContextAccessor? _httpContext;
    private readonly IOfficialOAuthApps? _apps;

    public OfficialOAuthBroker(
        IHttpClientFactory http,
        IConfiguration configuration,
        IHttpContextAccessor? httpContext = null,
        IOfficialOAuthApps? apps = null)
    {
        _http = http;
        _configuration = configuration;
        _httpContext = httpContext;
        _apps = apps;
    }

    public Task<AuthorizationStart> StartAsync(PlatformConnection connection, IPlatformAdapter adapter, CancellationToken cancellationToken)
    {
        var app = App(connection.PlatformCode);
        if (app is not null)
        {
            var redirect = RedirectUri();
            var popup = connection.PlatformCode is "FACEBOOK" or "INSTAGRAM" ? "&display=popup" : string.Empty;
            var url =
                $"{app.AuthorizeUrl}?response_type=code&client_id={Uri.EscapeDataString(app.ClientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirect)}" +
                $"&scope={Uri.EscapeDataString(app.Scopes)}" +
                $"&state={Uri.EscapeDataString(connection.AuthorizationState ?? string.Empty)}" +
                "&access_type=offline&prompt=consent" +
                popup;
            return Task.FromResult(new AuthorizationStart(url, false));
        }

        if (!string.IsNullOrWhiteSpace((_apps?.ApiKey(connection.PlatformCode) ?? OfficialOAuthCatalog.ApiKey(connection.PlatformCode, _configuration))) ||
            adapter.Describe().AuthMode == PlatformAuthMode.Assisted)
        {
            return Task.FromResult(new AuthorizationStart(string.Empty, true));
        }

        return Task.FromResult(new AuthorizationStart(string.Empty, false));
    }

    public async Task CompleteAsync(PlatformConnection connection, string? code, CancellationToken cancellationToken)
    {
        var app = App(connection.PlatformCode);
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

        var apiKey = _apps?.ApiKey(connection.PlatformCode) ?? OfficialOAuthCatalog.ApiKey(connection.PlatformCode, _configuration);
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

        connection.MarkError("Sign in on the official platform to connect. A development grant is not accepted.");
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

        var app = App(connection.PlatformCode);
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

        if (connection.PlatformCode is "FACEBOOK" or "INSTAGRAM")
        {
            var exchanged = await TryMetaLongLivedAsync(app, access, cancellationToken);
            if (exchanged is not null)
            {
                access = exchanged.Value.Access;
                expires = exchanged.Value.Expires;
            }
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

    private async Task<(string Access, DateTimeOffset? Expires)?> TryMetaLongLivedAsync(
        OAuthApp app,
        string shortLived,
        CancellationToken cancellationToken)
    {
        var url =
            "https://graph.facebook.com/v21.0/oauth/access_token" +
            $"?grant_type=fb_exchange_token&client_id={Uri.EscapeDataString(app.ClientId)}" +
            $"&client_secret={Uri.EscapeDataString(app.ClientSecret)}" +
            $"&fb_exchange_token={Uri.EscapeDataString(shortLived)}";
        try
        {
            using var response = await _http.CreateClient("official-platforms").GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode ||
                !TryReadAccessToken(body, app.Scopes, out var access, out _, out var expires, out _) ||
                string.IsNullOrWhiteSpace(access))
            {
                return null;
            }

            return (access, expires);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return null;
        }
    }

    private OAuthApp? App(string platformCode) =>
        _apps?.Resolve(platformCode) ?? OfficialOAuthCatalog.TryGet(platformCode, _configuration);

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

    private string RedirectUri()
    {
        if (_apps is not null)
        {
            return _apps.RedirectUri();
        }

        var configured = _configuration["Connections:RedirectUri"];
        var request = _httpContext?.HttpContext?.Request;
        if (request is not null)
        {
            var fromRequest = $"{request.Scheme}://{request.Host}/v1/connections/callback";
            if (string.IsNullOrWhiteSpace(configured))
            {
                return fromRequest;
            }

            var configuredIsLocal = configured.Contains("localhost", StringComparison.OrdinalIgnoreCase);
            var requestIsLocal = request.Host.Host is "localhost" or "127.0.0.1";
            return configuredIsLocal && !requestIsLocal ? fromRequest : configured;
        }

        return string.IsNullOrWhiteSpace(configured)
            ? "http://localhost:5088/v1/connections/callback"
            : configured;
    }
}
