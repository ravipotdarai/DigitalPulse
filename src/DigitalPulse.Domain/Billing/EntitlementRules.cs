using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Tenancy;

namespace DigitalPulse.Domain.Billing;

public static class EntitlementRules
{
    public static void EnsureCanAddBusiness(TenantType tenantType, SubscriptionPlan plan, int currentBusinessCount)
    {
        Subscription.EnsurePlanMatchesTenant(tenantType, plan);
        if (currentBusinessCount >= plan.MaxBusinesses)
        {
            throw new InvalidOperationException($"Plan {plan.Code} allows {plan.MaxBusinesses} business(es).");
        }
    }
}
