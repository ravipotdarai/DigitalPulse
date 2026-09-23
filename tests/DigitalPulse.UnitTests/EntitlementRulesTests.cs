using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Tenancy;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class EntitlementRulesTests
{
    [Fact]
    public void Starter_rejects_second_business()
    {
        var plan = SubscriptionPlan.Create("STARTER", "Starter", 2999m, 1, false, 1);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            EntitlementRules.EnsureCanAddBusiness(TenantType.Direct, plan, 1));
        Assert.Contains("1 business", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Direct_cannot_use_agency_plan()
    {
        var plan = SubscriptionPlan.Create("AGENCY", "Agency", 29999m, 100, true, 4);
        Assert.Throws<InvalidOperationException>(() =>
            Subscription.EnsurePlanMatchesTenant(TenantType.Direct, plan));
    }

    [Fact]
    public void Agency_plan_is_only_listed_for_agency_tenants()
    {
        var agency = SubscriptionPlan.Create("AGENCY", "Agency", 29999m, 100, true, 4);
        var starter = SubscriptionPlan.Create("STARTER", "Starter", 2999m, 1, false, 1);
        Assert.True(agency.IsAvailableTo(TenantType.Agency));
        Assert.False(agency.IsAvailableTo(TenantType.Direct));
        Assert.True(starter.IsAvailableTo(TenantType.Direct));
        Assert.False(starter.IsAvailableTo(TenantType.Agency));
    }
}
