using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Contracts.Billing;
using DigitalPulse.Domain.Billing;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Subscriptions;

public sealed class SelectPlanHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ISubscriptionPlanCatalog _catalog;

    public SelectPlanHandler(IAppDbContext db, ITenantContext tenantContext, ISubscriptionPlanCatalog catalog)
    {
        _db = db;
        _tenantContext = tenantContext;
        _catalog = catalog;
    }

    public async Task<SubscriptionResponse> Handle(SelectPlanRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var tenant = await _db.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
        var plan = await _catalog.GetByCodeAsync(request.PlanCode, cancellationToken)
            ?? throw AppException.NotFound("Subscription plan was not found.");

        try
        {
            Subscription.EnsurePlanMatchesTenant(tenant.Type, plan);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        var existing = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken);

        if (existing is null)
        {
            existing = Subscription.Start(tenantId, plan.Id);
            _db.Subscriptions.Add(existing);
        }
        else
        {
            existing.ChangePlan(plan.Id);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SubscriptionResponse(
            existing.Id,
            tenantId,
            plan.Code,
            plan.Name,
            plan.MonthlyPriceInr,
            plan.MaxBusinesses,
            existing.Status.ToString());
    }
}
