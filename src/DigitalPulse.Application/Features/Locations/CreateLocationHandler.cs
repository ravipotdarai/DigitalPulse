using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Contracts.Businesses;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Businesses;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Locations;

public sealed class CreateLocationHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public CreateLocationHandler(IAppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<LocationResponse> Handle(Guid businessId, CreateLocationRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var business = await _db.Businesses
            .FirstOrDefaultAsync(b => b.Id == businessId && b.TenantId == tenantId, cancellationToken)
            ?? throw AppException.NotFound("Business was not found.");

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial), cancellationToken);
        if (subscription is not null)
        {
            var plan = await _db.Plans.AsNoTracking().FirstAsync(p => p.Id == subscription.PlanId, cancellationToken);
            var used = await _db.Locations.CountAsync(l => l.TenantId == tenantId, cancellationToken);
            try
            {
                EntitlementRules.EnsureCanAddLocation(plan, used);
            }
            catch (InvalidOperationException ex)
            {
                throw AppException.Validation(ex.Message);
            }
        }

        var location = BusinessLocation.Create(
            tenantId,
            business.Id,
            request.Name,
            request.AddressLine,
            request.City,
            request.Region,
            request.PostalCode,
            request.CountryCode);

        _db.Locations.Add(location);
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
