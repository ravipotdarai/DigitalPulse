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

    [Fact]
    public void Starter_rejects_sixth_connection()
    {
        var plan = SubscriptionPlan.Create("STARTER", "Starter", 2999m, 1, false, 1, 5);
        var ex = Assert.Throws<InvalidOperationException>(() => EntitlementRules.EnsureCanAddConnection(plan, 5));
        Assert.Contains("5 connection", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Starter_rejects_third_scan_in_the_month()
    {
        var plan = SubscriptionPlan.Create("STARTER", "Starter", 2999m, 1, false, 1, 5, 2);
        var ex = Assert.Throws<InvalidOperationException>(() => EntitlementRules.EnsureCanRunScan(plan, 2));
        Assert.Contains("2 scan", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Starter_rejects_the_twenty_sixth_action_in_the_month()
    {
        var plan = SubscriptionPlan.Create("STARTER", "Starter", 2999m, 1, false, 1, 5, 2, 25);
        var ex = Assert.Throws<InvalidOperationException>(() => EntitlementRules.EnsureCanRunAction(plan, 25));
        Assert.Contains("25 action", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Starter_cannot_use_whatsapp()
    {
        var plan = SubscriptionPlan.Create("STARTER", "Starter", 2999m, 1, false, 1, 5, 2, 25, false, 0);
        var ex = Assert.Throws<InvalidOperationException>(() => EntitlementRules.EnsureCanUseWhatsApp(plan));
        Assert.Contains("does not include WhatsApp", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Starter_rejects_a_second_location()
    {
        var plan = SubscriptionPlan.Create("STARTER", "Starter", 2999m, 1, false, 1);
        var ex = Assert.Throws<InvalidOperationException>(() => EntitlementRules.EnsureCanAddLocation(plan, 1));
        Assert.Contains("1 location", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Starter_rejects_the_fifty_first_ai_run()
    {
        var plan = SubscriptionPlan.Create("STARTER", "Starter", 2999m, 1, false, 1);
        var ex = Assert.Throws<InvalidOperationException>(() => EntitlementRules.EnsureCanRunAi(plan, 50));
        Assert.Contains("50 AI", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cancelled_subscription_is_not_usable()
    {
        Assert.False(BillingPolicy.IsUsable(SubscriptionStatus.Cancelled));
        Assert.False(BillingPolicy.IsUsable(SubscriptionStatus.PastDue));
        Assert.True(BillingPolicy.IsUsable(SubscriptionStatus.Active));
        Assert.True(BillingPolicy.IsUsable(SubscriptionStatus.Trial));
    }
}
