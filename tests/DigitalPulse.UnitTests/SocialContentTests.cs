using DigitalPulse.Domain.Social;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class SocialContentTests
{
    [Fact]
    public void Development_hold_does_not_mark_published()
    {
        var item = SocialContentItem.Draft(Guid.NewGuid(), Guid.NewGuid(), "FACEBOOK", SocialContentKind.FacebookPost, "Weekend hours", "Open until 8.");
        item.Approve();
        item.MarkBlocked("Live provider publish waits for an official OAuth grant.");
        Assert.Equal(SocialContentStatus.Blocked, item.Status);
        Assert.Equal(SocialVerificationStatus.Hold, item.VerificationStatus);
        Assert.NotEqual(SocialContentStatus.Published, item.Status);
    }

    [Fact]
    public void Official_write_can_mark_published()
    {
        var item = SocialContentItem.Draft(Guid.NewGuid(), Guid.NewGuid(), "FACEBOOK", SocialContentKind.FacebookPost, "Weekend hours", "Open until 8.");
        item.Approve();
        item.MarkPublished("Official Facebook write accepted (200).");
        Assert.Equal(SocialContentStatus.Published, item.Status);
        Assert.Null(item.LastPublishError);
    }
}
