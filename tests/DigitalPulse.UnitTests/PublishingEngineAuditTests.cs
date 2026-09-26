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
}
