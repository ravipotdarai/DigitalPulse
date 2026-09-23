using DigitalPulse.Application.Abstractions;
using DigitalPulse.Contracts.Businesses;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Locations;

public sealed class ListLocationsHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ListLocationsHandler(IAppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<LocationResponse>> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        return await _db.Locations.AsNoTracking()
            .Where(l => l.TenantId == tenantId && l.BusinessId == businessId)
            .OrderBy(l => l.Name)
            .Select(l => new LocationResponse(
                l.Id,
                l.BusinessId,
                l.TenantId,
                l.Name,
                l.AddressLine,
                l.City,
                l.Region,
                l.PostalCode,
                l.CountryCode))
            .ToListAsync(cancellationToken);
    }
}
