using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Contracts.Onboarding;
using DigitalPulse.Domain.Billing;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Onboarding;

public sealed class GetOnboardingStatusHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantContext _tenantContext;

    public GetOnboardingStatusHandler(IAppDbContext db, ICurrentUser currentUser, ITenantContext tenantContext)
    {
        _db = db;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
    }

    public async Task<OnboardingStatusResponse> Handle(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw AppException.Unauthorized();
        }

        var hasTenant = _tenantContext.TenantId is not null;
        if (!hasTenant)
        {
            return new OnboardingStatusResponse(false, false, false, false, "tenant");
        }

        var tenantId = _tenantContext.TenantId!.Value;
        var hasBusiness = await _db.Businesses.AnyAsync(b => b.TenantId == tenantId, cancellationToken);
        var hasLocation = await _db.Locations.AnyAsync(l => l.TenantId == tenantId, cancellationToken);
        var hasSubscription = await _db.Subscriptions.AnyAsync(
            s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active,
            cancellationToken);

        var next = !hasBusiness ? "business"
            : !hasLocation ? "location"
            : !hasSubscription ? "plan"
            : "dashboard";

        return new OnboardingStatusResponse(true, hasBusiness, hasLocation, hasSubscription, next);
    }
}
