using DigitalPulse.Domain.Billing;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class BillingPolicyTests
{
    [Fact]
    public void Annual_price_comes_from_the_catalog()
    {
        var plan = SubscriptionPlan.Create("GROWTH", "Growth", 6999m, 3, false, 2, 15, 10, 150, true, 2000, 24, 5, 250, 5, 0, 10, false, 69990m);
        Assert.Equal(6999m, BillingPolicy.PriceFor(plan, BillingInterval.Monthly));
        Assert.Equal(69990m, BillingPolicy.PriceFor(plan, BillingInterval.Annual));
    }

    [Fact]
    public void Cancel_now_blocks_metered_work()
    {
        var subscription = Subscription.Start(Guid.NewGuid(), Guid.NewGuid());
        Assert.True(subscription.IsUsable);
        subscription.CancelNow("Stopped.");
        Assert.Equal(SubscriptionStatus.Cancelled, subscription.Status);
        Assert.False(subscription.IsUsable);
    }

    [Fact]
    public void Resume_restores_an_active_hold()
    {
        var subscription = Subscription.Start(Guid.NewGuid(), Guid.NewGuid());
        subscription.CancelNow("Stopped.");
        subscription.Resume();
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.True(subscription.IsUsable);
        Assert.Contains("capture", subscription.HoldReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Held_invoice_does_not_mark_itself_paid()
    {
        var invoice = Invoice.Issue(Guid.NewGuid(), Guid.NewGuid(), "INV-202609-01", BillingInterval.Monthly, 2999m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMonths(1), "Held.");
        Assert.Equal(InvoiceStatus.Held, invoice.Status);
        Assert.Null(invoice.PaidAtUtc);
    }
}
