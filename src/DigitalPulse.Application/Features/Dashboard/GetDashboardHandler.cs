using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Contracts.Onboarding;
using DigitalPulse.Domain.Billing;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Dashboard;

public sealed class GetDashboardHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetDashboardHandler(IAppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<DashboardResponse> Handle(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var tenant = await _db.Tenants.AsNoTracking().FirstAsync(t => t.Id == tenantId, cancellationToken);
        var subscription = await _db.Subscriptions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken)
            ?? throw AppException.Validation("Complete plan selection before opening the dashboard.");

        var plan = await _db.Plans.AsNoTracking().FirstAsync(p => p.Id == subscription.PlanId, cancellationToken);
        var businesses = await _db.Businesses.AsNoTracking()
            .Where(b => b.TenantId == tenantId)
            .Select(b => new DashboardBusinessResponse(
                b.Id,
                b.Name,
                b.Website,
                _db.Locations.Count(l => l.BusinessId == b.Id && l.TenantId == tenantId)))
            .ToListAsync(cancellationToken);

        return new DashboardResponse(
            tenant.Id,
            tenant.Name,
            tenant.Type.ToString(),
            plan.Name,
            plan.MaxBusinesses,
            businesses.Count,
            businesses.Sum(b => b.LocationCount),
            businesses);
    }
}
