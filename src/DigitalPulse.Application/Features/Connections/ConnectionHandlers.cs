using System.Net.Http;
using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Identity;
using DigitalPulse.Application.Website;
using DigitalPulse.Contracts.Connections;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Platforms;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Connections;

internal static class ConnectionMap
{
    public static PlatformCapabilityResponse Caps(PlatformCapabilities caps) =>
        new(caps.CanRead, caps.CanCreate, caps.CanUpdate, caps.CanDelete, caps.CanPublish, caps.CanGetMetrics, caps.AssistedOnly);

    public static PlatformCatalogItem Catalog(IPlatformAdapter adapter, bool officialLoginReady)
    {
        var d = adapter.Describe();
        return new(d.Code, d.Name, d.Category, d.AuthMode.ToString(), d.Summary, Caps(d.Capabilities), officialLoginReady);
    }

    public static ConnectionResponse ToResponse(this PlatformConnection connection, IPlatformAdapter adapter)
    {
        var d = adapter.Describe();
        return new(
            connection.Id,
            connection.BusinessId,
            d.Code,
            d.Name,
            d.Category,
            connection.Status.ToString(),
            connection.AuthMode.ToString(),
            connection.ExternalAccount,
            connection.GrantKind,
            connection.HasLiveCredential,
            connection.ConnectedAtUtc,
            connection.LastHealthAtUtc,
            connection.LastHealthStatus,
            connection.LastError,
            Caps(d.Capabilities));
    }

    public static bool IsSignedIn(PlatformConnection connection) =>
        connection.Status == ConnectionStatus.Connected &&
        (connection.HasLiveCredential ||
         string.Equals(connection.GrantKind, "Assisted", StringComparison.OrdinalIgnoreCase));
}

public sealed class GetConnectionCenterHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPlatformAdapterCatalog _catalog;
    private readonly IOfficialOAuthApps _oauth;

    public GetConnectionCenterHandler(IAppDbContext db, ITenantContext tenant, IPlatformAdapterCatalog catalog, IOfficialOAuthApps oauth)
    {
        _db = db;
        _tenant = tenant;
        _catalog = catalog;
        _oauth = oauth;
    }

    public async Task<ConnectionCenterResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var plan = await CurrentPlan(_db, tenantId, cancellationToken);
        var connections = await _db.Connections.AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        return new ConnectionCenterResponse(
            _catalog.All().Select(adapter =>
            {
                var mode = adapter.Describe().AuthMode;
                var ready = mode != PlatformAuthMode.OAuth || _oauth.Resolve(adapter.Describe().Code) is not null;
                return ConnectionMap.Catalog(adapter, ready);
            }).ToList(),
            connections.Select(c => c.ToResponse(_catalog.Get(c.PlatformCode))).ToList(),
            plan.MaxConnections,
            _oauth.Status());
    }

    internal static async Task<SubscriptionPlan> CurrentPlan(IAppDbContext db, Guid tenantId, CancellationToken cancellationToken)
    {
        var subscription = await db.Subscriptions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken)
            ?? throw AppException.Validation("Complete plan selection before connecting platforms.");
        return await db.Plans.AsNoTracking().FirstAsync(p => p.Id == subscription.PlanId, cancellationToken);
    }
}

public sealed class StartConnectionHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPlatformAdapterCatalog _catalog;
    private readonly IPlatformAuthorizationBroker _broker;

    public StartConnectionHandler(
        IAppDbContext db,
        ITenantContext tenant,
        IPlatformAdapterCatalog catalog,
        IPlatformAuthorizationBroker broker)
    {
        _db = db;
        _tenant = tenant;
        _catalog = catalog;
        _broker = broker;
    }

    public async Task<StartConnectionResponse> Handle(Guid businessId, string platformCode, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var adapter = _catalog.Get(platformCode);
        var descriptor = adapter.Describe();

        var existing = await _db.Connections.FirstOrDefaultAsync(
            c => c.BusinessId == businessId && c.PlatformCode == descriptor.Code, cancellationToken);
        if (existing is not null && ConnectionMap.IsSignedIn(existing))
        {
            throw AppException.Conflict($"{descriptor.Name} is already connected.");
        }

        var plan = await GetConnectionCenterHandler.CurrentPlan(_db, tenantId, cancellationToken);
        var count = await _db.Connections.CountAsync(c => c.BusinessId == businessId, cancellationToken);
        if (existing is null)
        {
            try
            {
                EntitlementRules.EnsureCanAddConnection(plan, count);
            }
            catch (InvalidOperationException ex)
            {
                throw AppException.Validation(ex.Message);
            }
        }

        var state = Guid.NewGuid().ToString("N");
        var connection = existing ?? PlatformConnection.Start(tenantId, businessId, descriptor.Code, descriptor.AuthMode, state);
        if (existing is null)
        {
            _db.Connections.Add(connection);
        }
        else
        {
            existing.BeginReauthorize(state);
            connection = existing;
        }

        var start = await _broker.StartAsync(connection, adapter, cancellationToken);
        if (start.CompleteInPlace)
        {
            await _broker.CompleteAsync(connection, "development", cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        var needsApp = !start.CompleteInPlace &&
                       string.IsNullOrWhiteSpace(start.AuthorizationUrl) &&
                       descriptor.AuthMode == PlatformAuthMode.OAuth;
        return new StartConnectionResponse(connection.ToResponse(adapter), start.CompleteInPlace ? null : start.AuthorizationUrl, start.CompleteInPlace, needsApp);
    }
}

public sealed class CompleteConnectionHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPlatformAdapterCatalog _catalog;
    private readonly IPlatformAuthorizationBroker _broker;

    public CompleteConnectionHandler(
        IAppDbContext db,
        ITenantContext tenant,
        IPlatformAdapterCatalog catalog,
        IPlatformAuthorizationBroker broker)
    {
        _db = db;
        _tenant = tenant;
        _catalog = catalog;
        _broker = broker;
    }

    public async Task<ConnectionResponse> Handle(Guid businessId, Guid connectionId, string? code, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var connection = await _db.Connections.FirstOrDefaultAsync(c => c.Id == connectionId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Connection was not found.");
        var adapter = _catalog.Get(connection.PlatformCode);
        await _broker.CompleteAsync(connection, code, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return connection.ToResponse(adapter);
    }
}

public sealed class CompleteConnectionByStateHandler
{
    private readonly IAppDbContext _db;
    private readonly IPlatformAdapterCatalog _catalog;
    private readonly IPlatformAuthorizationBroker _broker;

    public CompleteConnectionByStateHandler(IAppDbContext db, IPlatformAdapterCatalog catalog, IPlatformAuthorizationBroker broker)
    {
        _db = db;
        _catalog = catalog;
        _broker = broker;
    }

    public async Task<string> Handle(string state, string? code, CancellationToken cancellationToken)
    {
        var connection = await _db.FindConnectionByStateAsync(state, cancellationToken)
            ?? throw AppException.NotFound("Authorization state was not found.");
        await _broker.CompleteAsync(connection, code, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return connection.PlatformCode;
    }
}

public sealed class ConnectionActionHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPlatformAdapterCatalog _catalog;
    private readonly IPlatformAuthorizationBroker _broker;
    private readonly IOfficialPlatformGateway _gateway;
    private readonly ILiveTokenRefresher _tokens;

    public ConnectionActionHandler(
        IAppDbContext db,
        ITenantContext tenant,
        IPlatformAdapterCatalog catalog,
        IPlatformAuthorizationBroker broker,
        IOfficialPlatformGateway gateway,
        ILiveTokenRefresher tokens)
    {
        _db = db;
        _tenant = tenant;
        _catalog = catalog;
        _broker = broker;
        _gateway = gateway;
        _tokens = tokens;
    }

    public async Task<ConnectionResponse> HealthAsync(Guid businessId, Guid connectionId, CancellationToken cancellationToken)
    {
        var (connection, adapter) = await Load(businessId, connectionId, cancellationToken);
        var health = await adapter.HealthCheckAsync(connection, cancellationToken);
        connection.RecordHealth(health.Status, health.Detail);
        if (health.Status == "NeedsReauth") connection.MarkNeedsReauth(health.Detail);
        await _db.SaveChangesAsync(cancellationToken);
        return connection.ToResponse(adapter);
    }

    public async Task<IReadOnlyList<DiagnosticResponse>> DiagnoseAsync(Guid businessId, Guid connectionId, CancellationToken cancellationToken)
    {
        var (connection, adapter) = await Load(businessId, connectionId, cancellationToken);
        var checks = await adapter.DiagnoseAsync(connection, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return checks.Select(c => new DiagnosticResponse(c.Check, c.Status, c.Detail)).ToList();
    }

    public async Task<StartConnectionResponse> ReauthorizeAsync(Guid businessId, Guid connectionId, CancellationToken cancellationToken)
    {
        var (connection, adapter) = await Load(businessId, connectionId, cancellationToken);
        connection.BeginReauthorize(Guid.NewGuid().ToString("N"));
        var start = await _broker.StartAsync(connection, adapter, cancellationToken);
        if (start.CompleteInPlace)
        {
            await _broker.CompleteAsync(connection, "development", cancellationToken);
        }
        await _db.SaveChangesAsync(cancellationToken);
        var needsApp = !start.CompleteInPlace && string.IsNullOrWhiteSpace(start.AuthorizationUrl);
        return new StartConnectionResponse(connection.ToResponse(adapter), start.CompleteInPlace ? null : start.AuthorizationUrl, start.CompleteInPlace, needsApp);
    }

    public async Task DisconnectAsync(Guid businessId, Guid connectionId, CancellationToken cancellationToken)
    {
        var (connection, _) = await Load(businessId, connectionId, cancellationToken);
        _db.Connections.Remove(connection);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConnectionAccountOption>> ListAccountsAsync(Guid businessId, Guid connectionId, CancellationToken cancellationToken)
    {
        var (connection, _) = await Load(businessId, connectionId, cancellationToken);
        await _tokens.EnsureFreshAsync(connection, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        var choices = await ListOfficialAsync(connection, cancellationToken);
        return OfficialAccounts.Public(choices);
    }

    internal async Task<IReadOnlyList<OfficialAccountChoice>> ListOfficialAsync(
        PlatformConnection connection,
        CancellationToken cancellationToken)
    {
        if (!connection.HasLiveCredential)
        {
            return [];
        }

        var code = connection.PlatformCode.ToUpperInvariant();
        return code switch
        {
            "FACEBOOK" => await OfficialGet(
                connection,
                "https://graph.facebook.com/v21.0/me/accounts?fields=id,name,access_token",
                OfficialAccounts.FacebookPages,
                cancellationToken),
            "INSTAGRAM" => await OfficialGet(
                connection,
                "https://graph.facebook.com/v21.0/me/accounts?fields=id,name,access_token,instagram_business_account{id,username}",
                OfficialAccounts.InstagramAccounts,
                cancellationToken),
            "YOUTUBE" => await OfficialGet(
                connection,
                "https://www.googleapis.com/youtube/v3/channels?part=snippet&mine=true",
                OfficialAccounts.YouTubeChannels,
                cancellationToken),
            "SEARCH_CONSOLE" => await OfficialGet(
                connection,
                "https://searchconsole.googleapis.com/webmasters/v3/sites",
                OfficialAccounts.SearchConsoleSites,
                cancellationToken),
            "GOOGLE_ANALYTICS" => await OfficialGet(
                connection,
                "https://analyticsadmin.googleapis.com/v1beta/accountSummaries",
                body => AnalyticsProperties.Parse(body).Select(p => new OfficialAccountChoice(p.Property, p.Label, "GA4", null)).ToList(),
                cancellationToken),
            "GOOGLE" => await GoogleLocationsAsync(connection, cancellationToken),
            "LINKEDIN" => await OfficialGet(
                connection,
                "https://api.linkedin.com/v2/userinfo",
                OfficialAccounts.LinkedInPerson,
                cancellationToken),
            _ => []
        };
    }

    private async Task<IReadOnlyList<OfficialAccountChoice>> OfficialGet(
        PlatformConnection connection,
        string url,
        Func<string?, IReadOnlyList<OfficialAccountChoice>> parse,
        CancellationToken cancellationToken)
    {
        var result = await _gateway.SendAsync(HttpMethod.Get, url, connection.AccessToken, null, null, cancellationToken);
        return result.Ok ? parse(result.Body) : [];
    }

    private async Task<IReadOnlyList<OfficialAccountChoice>> GoogleLocationsAsync(
        PlatformConnection connection,
        CancellationToken cancellationToken)
    {
        var accounts = await OfficialGet(
            connection,
            "https://mybusinessaccountmanagement.googleapis.com/v1/accounts",
            OfficialAccounts.GoogleAccounts,
            cancellationToken);
        var locations = new List<OfficialAccountChoice>();
        foreach (var account in accounts.Take(8))
        {
            var result = await _gateway.SendAsync(
                HttpMethod.Get,
                $"https://mybusinessbusinessinformation.googleapis.com/v1/{account.Id}/locations?readMask=name,title",
                connection.AccessToken,
                null,
                null,
                cancellationToken);
            if (result.Ok)
            {
                locations.AddRange(OfficialAccounts.GoogleLocations(result.Body));
            }
        }

        return locations.Count > 0 ? locations : accounts;
    }

    public async Task<ConnectionResponse> SelectAccountAsync(Guid businessId, Guid connectionId, string? externalAccount, CancellationToken cancellationToken)
    {
        var (connection, adapter) = await Load(businessId, connectionId, cancellationToken);
        if (!connection.HasLiveCredential)
        {
            throw AppException.Validation("Pick an official account after a live OAuth grant. DigitalPulse will not invent a property.");
        }

        var options = await ListOfficialAsync(connection, cancellationToken);
        var chosen = options.FirstOrDefault(o => o.Id.Equals(externalAccount?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (chosen is null)
        {
            throw AppException.Validation("The selected account was not returned by the official API.");
        }

        connection.AssignExternalAccount(chosen.Id);
        if (!string.IsNullOrWhiteSpace(chosen.AccessToken))
        {
            connection.ApplyRefreshedTokens(chosen.AccessToken, connection.RefreshToken, connection.TokenExpiresAtUtc, connection.TokenScope);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return connection.ToResponse(adapter);
    }

    private async Task<(PlatformConnection Connection, IPlatformAdapter Adapter)> Load(
        Guid businessId, Guid connectionId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var connection = await _db.Connections.FirstOrDefaultAsync(c => c.Id == connectionId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Connection was not found.");
        return (connection, _catalog.Get(connection.PlatformCode));
    }
}

public sealed class GetOfficialOAuthAppsHandler(IOfficialOAuthApps apps)
{
    public OfficialOAuthAppsStatus Handle() => apps.Status();
}

public sealed class SaveOfficialOAuthAppsHandler(IOfficialOAuthApps apps)
{
    public async Task<OfficialOAuthAppsStatus> Handle(SaveOfficialOAuthAppsRequest request, CancellationToken cancellationToken)
    {
        await apps.SaveAsync(request, cancellationToken);
        return apps.Status();
    }
}
