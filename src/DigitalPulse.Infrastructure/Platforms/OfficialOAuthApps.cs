using DigitalPulse.Application.Abstractions;
using DigitalPulse.Contracts.Connections;
using DigitalPulse.Domain.Platforms;
using DigitalPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalPulse.Infrastructure.Platforms;

public sealed class OfficialOAuthApps : IOfficialOAuthApps
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopes;
    private readonly IHttpContextAccessor? _httpContext;

    public OfficialOAuthApps(
        IConfiguration configuration,
        IServiceScopeFactory scopes,
        IHttpContextAccessor? httpContext = null)
    {
        _configuration = configuration;
        _scopes = scopes;
        _httpContext = httpContext;
    }

    public OAuthApp? Resolve(string platformCode)
    {
        var provider = HostOAuthApp.Normalize(platformCode);
        var stored = Stored(provider);
        return OfficialOAuthCatalog.TryGet(
            platformCode,
            stored?.ClientId ?? ConfigId(provider),
            stored?.ClientSecret ?? ConfigSecret(provider));
    }

    public string? ApiKey(string platformCode) => OfficialOAuthCatalog.ApiKey(platformCode, _configuration);

    public OfficialOAuthAppsStatus Status()
    {
        var redirect = RedirectUri();
        return new OfficialOAuthAppsStatus(redirect,
        [
            State("GOOGLE"),
            State("META"),
            State("LINKEDIN")
        ]);
    }

    public async Task SaveAsync(SaveOfficialOAuthAppsRequest request, CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await UpsertAsync(db, "GOOGLE", request.GoogleClientId, request.GoogleClientSecret, cancellationToken);
        await UpsertAsync(db, "META", request.MetaClientId, request.MetaClientSecret, cancellationToken);
        await UpsertAsync(db, "LINKEDIN", request.LinkedInClientId, request.LinkedInClientSecret, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private OfficialOAuthAppState State(string provider)
    {
        var stored = Stored(provider);
        var id = stored?.ClientId ?? ConfigId(provider);
        return new OfficialOAuthAppState(provider, !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(stored?.ClientSecret ?? ConfigSecret(provider)), Mask(id));
    }

    private HostOAuthApp? Stored(string provider)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return db.HostOAuthApps.AsNoTracking().FirstOrDefault(row => row.Provider == provider);
    }

    private static async Task UpsertAsync(
        AppDbContext db,
        string provider,
        string? clientId,
        string? clientSecret,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return;
        }

        var row = await db.HostOAuthApps.FirstOrDefaultAsync(item => item.Provider == provider, cancellationToken);
        if (row is null)
        {
            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                throw Application.Common.AppException.Validation($"Give the official {provider} client secret with the client id.");
            }

            db.HostOAuthApps.Add(HostOAuthApp.Create(provider, clientId, clientSecret));
            return;
        }

        row.Update(clientId, clientSecret);
    }

    private string? ConfigId(string provider) => provider switch
    {
        "GOOGLE" => _configuration["Connections:Google:ClientId"],
        "META" => _configuration["Connections:Meta:ClientId"] ?? _configuration["Connections:Facebook:ClientId"],
        "LINKEDIN" => _configuration["Connections:LinkedIn:ClientId"],
        _ => null
    };

    private string? ConfigSecret(string provider) => provider switch
    {
        "GOOGLE" => _configuration["Connections:Google:ClientSecret"],
        "META" => _configuration["Connections:Meta:ClientSecret"] ?? _configuration["Connections:Facebook:ClientSecret"],
        "LINKEDIN" => _configuration["Connections:LinkedIn:ClientSecret"],
        _ => null
    };

    private static string? Mask(string? clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId)) return null;
        return clientId.Length <= 8 ? "••••" : $"{clientId[..4]}…{clientId[^4..]}";
    }

    public string RedirectUri()
    {
        var configured = _configuration["Connections:RedirectUri"];
        var request = _httpContext?.HttpContext?.Request;
        if (request is not null)
        {
            var fromRequest = $"{request.Scheme}://{request.Host}/v1/connections/callback";
            if (string.IsNullOrWhiteSpace(configured)) return fromRequest;
            var configuredIsLocal = configured.Contains("localhost", StringComparison.OrdinalIgnoreCase);
            var requestIsLocal = request.Host.Host is "localhost" or "127.0.0.1";
            return configuredIsLocal && !requestIsLocal ? fromRequest : configured;
        }

        return string.IsNullOrWhiteSpace(configured)
            ? "http://localhost:5088/v1/connections/callback"
            : configured;
    }
}
