using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Contracts.Businesses;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Locations;

public sealed class UpdateLocationHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public UpdateLocationHandler(IAppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<LocationResponse> Handle(
        Guid businessId,
        Guid locationId,
        CreateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var location = await _db.Locations.FirstOrDefaultAsync(
            l => l.Id == locationId && l.BusinessId == businessId && l.TenantId == tenantId,
            cancellationToken) ?? throw AppException.NotFound("Location was not found.");

        location.Update(
            request.Name,
            request.AddressLine,
            request.City,
            request.Region,
            request.PostalCode,
            request.CountryCode);
        await _db.SaveChangesAsync(cancellationToken);

        return new LocationResponse(
            location.Id,
            location.BusinessId,
            location.TenantId,
            location.Name,
            location.AddressLine,
            location.City,
            location.Region,
            location.PostalCode,
            location.CountryCode);
    }
}
