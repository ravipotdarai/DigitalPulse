using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Identity;
using DigitalPulse.Contracts.Connections;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Platforms;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Connections;

internal static class ConnectionMap
{
    public static PlatformCapabilityResponse Caps(PlatformCapabilities caps) =>
        new(caps.CanRead, caps.CanCreate, caps.CanUpdate, caps.CanDelete, caps.CanPublish, caps.CanGetMetrics, caps.AssistedOnly);

    public static PlatformCatalogItem Catalog(IPlatformAdapter adapter)
    {
        var d = adapter.Describe();
        return new(d.Code, d.Name, d.Category, d.AuthMode.ToString(), d.Summary, Caps(d.Capabilities));
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
            connection.ConnectedAtUtc,
            connection.LastHealthAtUtc,
            connection.LastHealthStatus,
            connection.LastError,
            Caps(d.Capabilities));
    }
}

public sealed class GetConnectionCenterHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPlatformAdapterCatalog _catalog;

    public GetConnectionCenterHandler(IAppDbContext db, ITenantContext tenant, IPlatformAdapterCatalog catalog)
    {
        _db = db;
        _tenant = tenant;
        _catalog = catalog;
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
            _catalog.All().Select(ConnectionMap.Catalog).ToList(),
            connections.Select(c => c.ToResponse(_catalog.Get(c.PlatformCode))).ToList(),
            plan.MaxConnections);
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
        if (existing is { Status: ConnectionStatus.Connected })
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
        return new StartConnectionResponse(connection.ToResponse(adapter), start.CompleteInPlace ? null : start.AuthorizationUrl, start.CompleteInPlace);
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

    public ConnectionActionHandler(
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
        return new StartConnectionResponse(connection.ToResponse(adapter), start.CompleteInPlace ? null : start.AuthorizationUrl, start.CompleteInPlace);
    }

    public async Task DisconnectAsync(Guid businessId, Guid connectionId, CancellationToken cancellationToken)
    {
        var (connection, _) = await Load(businessId, connectionId, cancellationToken);
        _db.Connections.Remove(connection);
        await _db.SaveChangesAsync(cancellationToken);
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
