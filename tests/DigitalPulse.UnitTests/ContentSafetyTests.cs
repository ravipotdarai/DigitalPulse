using DigitalPulse.Application.Features.Social;
using DigitalPulse.Domain.Safety;
using DigitalPulse.Domain.Social;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class ContentSafetyTests
{
    [Fact]
    public void Sexual_language_is_banned()
    {
        var result = ContentSafety.Assess("Weekend special", "Free porn tonight");
        Assert.False(result.Allowed);
        Assert.Contains("banned", result.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Throws<InvalidOperationException>(() =>
            SocialContentItem.Draft(Guid.NewGuid(), Guid.NewGuid(), "FACEBOOK", SocialContentKind.FacebookPost, "Sale", "Free porn tonight"));
    }

    [Fact]
    public void Ordinary_copy_is_allowed()
    {
        var result = ContentSafety.Assess("Weekend hours", "Open until 8 on Saturday.");
        Assert.True(result.Allowed);
    }

    [Fact]
    public void Seo_and_analytics_are_honest()
    {
        var review = SocialPostChecks.Evaluate("Hi", "Shop", false);
        Assert.Equal("Needs work", review.SeoStatus);
        Assert.Contains(review.SeoNotes, note => note.Contains("Title", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Not connected", review.AnalyticsStatus);
        Assert.Contains("will not invent", SocialPostChecks.Evaluate("Weekend bakery hours", "The bakery on MG Road is open until 8. Fresh loaves daily.", true).AnalyticsDetail, StringComparison.OrdinalIgnoreCase);
    }
}
