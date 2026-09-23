using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Contracts.Businesses;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Businesses;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Businesses;

public sealed class CreateBusinessHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public CreateBusinessHandler(IAppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<BusinessResponse> Handle(CreateBusinessRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var tenant = await _db.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
        var currentCount = await _db.Businesses.CountAsync(b => b.TenantId == tenantId, cancellationToken);

        var subscription = await _db.Subscriptions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken);

        if (subscription is not null)
        {
            var plan = await _db.Plans.AsNoTracking().FirstAsync(p => p.Id == subscription.PlanId, cancellationToken);
            EntitlementRules.EnsureCanAddBusiness(tenant.Type, plan, currentCount);
        }
        else if (currentCount >= 1)
        {
            throw AppException.Validation("Select a subscription plan before adding another business.");
        }

        var business = Business.Create(tenantId, request.Name, request.Website);
        _db.Businesses.Add(business);
        await _db.SaveChangesAsync(cancellationToken);
        return new BusinessResponse(business.Id, business.TenantId, business.Name, business.Website);
    }
}
