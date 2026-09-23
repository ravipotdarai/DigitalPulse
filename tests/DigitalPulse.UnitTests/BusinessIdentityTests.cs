using DigitalPulse.Domain.Businesses;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class BusinessIdentityTests
{
    [Fact]
    public void Approved_facts_can_publish_restricted_cannot()
    {
        var tenantId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var approved = BusinessFact.Create(tenantId, businessId, "CLAIM", "Family owned since 1998", FactStatus.Approved);
        var restricted = BusinessFact.Create(tenantId, businessId, "GSTIN", "27AAAAA0000A1Z5", FactStatus.Restricted);

        Assert.True(approved.CanPublish);
        Assert.False(restricted.CanPublish);
    }

    [Fact]
    public void Email_contact_requires_at_sign()
    {
        Assert.Throws<ArgumentException>(() =>
            ContactPoint.Create(Guid.NewGuid(), Guid.NewGuid(), ContactPointKind.Email, "not-an-email", null));
    }

    [Fact]
    public void Customer_mobile_is_separate_from_business_contacts()
    {
        var tenantId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var customer = Customer.Create(tenantId, businessId, "Ananya", null);
        var mobile = customer.AddContact(CustomerContactKind.Mobile, "9876543210");

        Assert.Equal(customer.Id, mobile.CustomerId);
        Assert.Equal(CustomerContactKind.Mobile, mobile.Kind);
    }
}
