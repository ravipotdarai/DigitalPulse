using DigitalPulse.Domain.Social;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class SocialContentTests
{
    [Fact]
    public void Publish_states_never_become_live_posts()
    {
        var item = SocialContentItem.Draft(Guid.NewGuid(), Guid.NewGuid(), "FACEBOOK", SocialContentKind.FacebookPost, "Weekend hours", "Open until 8.");
        item.Approve();
        item.MarkBlocked("Live provider publish is not wired.");
        Assert.Equal(SocialContentStatus.Blocked, item.Status);
        Assert.Equal(SocialVerificationStatus.Hold, item.VerificationStatus);
        Assert.DoesNotContain("Published", item.Status.ToString(), StringComparison.Ordinal);
    }
}
