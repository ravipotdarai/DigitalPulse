using DigitalPulse.Domain.Content;
using DigitalPulse.Domain.Projects;
using DigitalPulse.Infrastructure.Platforms;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class PublishingEngineAuditTests
{
    [Fact]
    public void Canonical_item_and_distribution_already_exist()
    {
        Assert.Contains(typeof(ContentItem).Assembly.GetTypes(), type => type.Name == "ContentItem");
        Assert.Contains(typeof(ContentDistribution).Assembly.GetTypes(), type => type.Name == "ContentDistribution");
        Assert.Contains(typeof(ContentVariant).Assembly.GetTypes(), type => type.Name == "ContentVariant");
    }

    [Fact]
    public void Website_adapter_does_not_claim_a_cms_write()
    {
        var website = new WebsiteAdapter().Describe();
        Assert.Equal("WEBSITE", website.Code);
        Assert.False(website.Capabilities.CanPublish);
        Assert.True(website.Capabilities.AssistedOnly);
    }

    [Fact]
    public void Distribution_retry_increments_attempts_and_never_invents_published()
    {
        var row = ContentDistribution.Start(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "GOOGLE", null, null, Guid.NewGuid(), "gbp-pune");
        row.Hold("Official write is not confirmed.");
        row.Retry();
        Assert.Equal(2, row.AttemptCount);
        Assert.Equal(ContentDistributionStatus.ApprovalRequired, row.Status);
        Assert.NotEqual(ContentDistributionStatus.Published, row.Status);
    }
}
