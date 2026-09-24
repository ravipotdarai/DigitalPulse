using DigitalPulse.Application.Abstractions;
using DigitalPulse.Contracts.Billing;
using DigitalPulse.Domain.Tenancy;

namespace DigitalPulse.Application.Features.Plans;

public sealed class ListPlansHandler
{
    private readonly ISubscriptionPlanCatalog _catalog;
    private readonly ITenantContext _tenantContext;
    private readonly IAppDbContext _db;

    public ListPlansHandler(ISubscriptionPlanCatalog catalog, ITenantContext tenantContext, IAppDbContext db)
    {
        _catalog = catalog;
        _tenantContext = tenantContext;
        _db = db;
    }

    public async Task<IReadOnlyList<PlanResponse>> Handle(CancellationToken cancellationToken)
    {
        IReadOnlyList<Domain.Billing.SubscriptionPlan> plans;
        if (_tenantContext.TenantId is { } tenantId)
        {
            var tenant = await _db.Tenants.FindAsync([tenantId], cancellationToken)
                ?? throw Common.AppException.NotFound("Tenant was not found.");
            plans = await _catalog.ListForTenantTypeAsync(tenant.Type, cancellationToken);
        }
        else
        {
            plans = await _catalog.ListAsync(cancellationToken);
        }

        return plans
            .Select(p => new PlanResponse(
                p.Id,
                p.Code,
                p.Name,
                p.MonthlyPriceInr,
                p.AnnualPriceInr,
                p.MaxBusinesses,
                p.MaxLocations,
                p.MaxConnections,
                p.ScansPerMonth,
                p.ActionsPerMonth,
                p.AiGenerationsPerMonth,
                p.MaxUsers,
                p.MaxAgencyClients,
                p.StorageGb,
                p.WhiteLabel,
                p.WhatsAppEnabled,
                p.WhatsAppMessagesPerMonth,
                p.MonitoringIntervalHours,
                p.AgencyOnly))
            .ToList();
    }
}
