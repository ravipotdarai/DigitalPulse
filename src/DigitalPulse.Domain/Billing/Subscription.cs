using DigitalPulse.Domain.Common;
using DigitalPulse.Domain.Tenancy;

namespace DigitalPulse.Domain.Billing;

public sealed class Subscription : TenantOwnedEntity
{
    public Guid PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; }

    private Subscription() { }

    public static Subscription Start(Guid tenantId, Guid planId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (planId == Guid.Empty) throw new ArgumentException("Plan is required.", nameof(planId));

        return new Subscription
        {
            TenantId = tenantId,
            PlanId = planId,
            Status = SubscriptionStatus.Active
        };
    }

    public void ChangePlan(Guid planId)
    {
        if (planId == Guid.Empty) throw new ArgumentException("Plan is required.", nameof(planId));
        PlanId = planId;
        Touch();
    }

    public static void EnsurePlanMatchesTenant(TenantType tenantType, SubscriptionPlan plan)
    {
        if (plan.AgencyOnly && tenantType != TenantType.Agency)
        {
            throw new InvalidOperationException("The Agency plan can only be assigned to Agency tenants.");
        }

        if (!plan.AgencyOnly && tenantType == TenantType.Agency)
        {
            throw new InvalidOperationException("Agency tenants must use the Agency plan.");
        }
    }
}
