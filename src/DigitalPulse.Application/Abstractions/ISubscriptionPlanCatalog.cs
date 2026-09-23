using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Tenancy;

namespace DigitalPulse.Application.Abstractions;

public interface ISubscriptionPlanCatalog
{
    Task<IReadOnlyList<SubscriptionPlan>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SubscriptionPlan>> ListForTenantTypeAsync(TenantType tenantType, CancellationToken cancellationToken);
    Task<SubscriptionPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken);
}
