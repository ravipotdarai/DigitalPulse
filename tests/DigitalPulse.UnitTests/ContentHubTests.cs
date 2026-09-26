using DigitalPulse.Domain.Content;
using DigitalPulse.Domain.Projects;
using DigitalPulse.Domain.Safety;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class ContentHubTests
{
    [Fact]
    public void Slug_is_unique_shape_from_title()
    {
        Assert.Equal("how-to-design-a-conference-room", ContentSlug.From(null, "How to Design a Conference Room"));
        Assert.Equal("custom-slug", ContentSlug.From("Custom Slug!", "Ignored"));
    }

    [Fact]
    public void Safety_blocks_banned_copy_on_hub_draft()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ContentItem.Draft(Guid.NewGuid(), Guid.NewGuid(), "ARTICLE", "Sale", null, "Weekend", "Free porn tonight", ContentVisibility.Private, null, null, null, null));
    }

    [Fact]
    public void Seo_score_is_only_the_check_fraction()
    {
        var thin = ContentSeo.Evaluate("Hi", "Shop", "Buy", null, null);
        Assert.Equal(thin.Total == 0 ? 0 : (int)Math.Round(100d * thin.Passed / thin.Total), thin.Score);
        Assert.InRange(thin.Score, 0, 100);
        Assert.NotEqual(87, thin.Score);

        var ready = ContentSeo.Evaluate(
            "How to choose a conference room system",
            "A practical guide to choosing a conference room system from the stored service record.",
            "# How to choose\n\nA conference room system should match the room size, the display, and the stored service the business already offers. Keep sentences short so the checklist can score readability without inventing a vanity number.",
            "conference",
            "https://example.com/guide");
        Assert.Equal((int)Math.Round(100d * ready.Passed / ready.Total), ready.Score);
        Assert.True(ready.Score > thin.Score);
        Assert.Equal(0, ready.EntityCoverageScore);
        Assert.InRange(ready.ReadabilityScore, 0, 100);
        Assert.InRange(ready.AeoScore, 0, 100);
        Assert.NotEqual(87, ready.ReadabilityScore);
        Assert.NotEqual(87, ready.AeoScore);
    }

    [Fact]
    public void Aeo_score_is_100_only_when_the_body_has_a_question_and_steps()
    {
        var thin = ContentSeo.Evaluate("How to choose", "A practical excerpt for the hub checklist snippet.", "No structure here at all.", null, null, "how-to-choose");
        Assert.Equal(0, thin.AeoScore);

        var structured = ContentSeo.Evaluate(
            "How to choose a conference room system",
            "A practical guide to choosing a conference room system from the stored service record.",
            "What should you check?\n\nFAQ\n1. Room size.\n2. Display size.\nA conference room system should match the stored service.",
            "conference",
            "https://example.com/guide",
            "how-to-choose-a-conference-room-system");
        Assert.Equal(100, structured.AeoScore);
        Assert.True(structured.SlugScore >= 50);
    }

    [Fact]
    public void FromProject_still_builds_a_private_pack()
    {
        var item = ContentItem.FromProject(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Harbour Roast");
        Assert.Equal("PROJECT_STORY", item.ContentTypeCode);
        Assert.Equal(ContentVisibility.Private, item.Visibility);
        Assert.NotNull(item.ProjectId);
        Assert.Equal("harbour-roast", item.Slug);
        Assert.DoesNotContain("AI generated", item.SourceNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Publish_requires_approval()
    {
        var item = ContentItem.Draft(Guid.NewGuid(), Guid.NewGuid(), "GUIDE", "How to choose a conference room", null, "A practical excerpt for the hub checklist snippet.", "# Guide\n\nBody long enough for the draft factory.", ContentVisibility.Public, null, null, null, null);
        Assert.Throws<InvalidOperationException>(() => item.Publish());
        item.RequestApproval();
        item.MarkApproved();
        item.Publish();
        Assert.Equal(ContentItemStatus.Published, item.Status);
        Assert.NotNull(item.PublishedAtUtc);
    }

    [Fact]
    public void Opportunity_score_is_the_inverse_of_coverage()
    {
        var opportunity = ContentOpportunity.Open(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ContentOpportunitySource.Service, 40, "Stored service.");
        Assert.Equal(40, opportunity.CoverageScore);
        Assert.Equal(60, opportunity.OpportunityScore);
        Assert.Equal(100, opportunity.RelevanceScore);
        Assert.Null(opportunity.CompetitionScore);
        Assert.Equal(2, opportunity.Priority);
    }

    [Fact]
    public void Schedule_requires_approval()
    {
        var item = ContentItem.Draft(Guid.NewGuid(), Guid.NewGuid(), "GUIDE", "How to choose a conference room", null, "A practical excerpt for the hub checklist snippet.", "# Guide\n\nBody long enough for the draft factory.", ContentVisibility.Public, null, null, null, null);
        Assert.Throws<InvalidOperationException>(() => item.Schedule(DateTimeOffset.UtcNow.AddDays(1)));
        item.RequestApproval();
        item.MarkApproved();
        item.Schedule(DateTimeOffset.UtcNow.AddDays(1));
        Assert.Equal(ContentItemStatus.Scheduled, item.Status);
        item.ClearSchedule();
        Assert.Equal(ContentItemStatus.Approved, item.Status);
    }

    [Fact]
    public void Search_url_is_public_only_after_publish()
    {
        var businessId = Guid.NewGuid();
        var item = ContentItem.Draft(Guid.NewGuid(), businessId, "GUIDE", "How to choose a conference room", "conference-room", "A practical excerpt for the hub checklist snippet.", "# Guide\n\nBody long enough for the draft factory.", ContentVisibility.Public, null, null, null, null);
        Assert.StartsWith("hub://", ContentHubPaths.SearchUrl(item.BusinessId, item.Id, item.Slug, item.Status, item.Visibility));
        item.RequestApproval();
        item.MarkApproved();
        item.Publish();
        Assert.Equal($"/hub/{businessId:D}/conference-room", ContentHubPaths.SearchUrl(item.BusinessId, item.Id, item.Slug, item.Status, item.Visibility));
    }

    [Fact]
    public void Ordinary_hub_copy_is_allowed()
    {
        Assert.True(ContentSafety.Assess("How to choose a conference room", "A practical guide from the stored service.").Allowed);
    }

    [Fact]
    public void Markup_round_trips_editorial_blocks()
    {
        var businessId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var body = string.Join("\n\n",
            "# How to choose",
            "A conference room system should match the stored service.",
            $"[Related guide](/hub/{businessId:D}/public-guide)",
            "- Room size",
            "- Display size",
            "> Keep the claim inside the stored record.",
            "![Hero](https://example.com/hero.jpg)",
            "!video[Walkthrough](https://www.youtube.com/watch?v=abcdefghijk)");
        var parsed = ContentMarkup.Parse(body);
        Assert.Equal(ContentBlockKind.Heading, parsed[0].Kind);
        Assert.Equal(ContentBlockKind.List, parsed.First(block => block.Kind == ContentBlockKind.List).Kind);
        Assert.Equal(ContentBlockKind.Image, parsed.First(block => block.Kind == ContentBlockKind.Image).Kind);
        Assert.Equal(ContentBlockKind.Video, parsed.First(block => block.Kind == ContentBlockKind.Video).Kind);
        Assert.Equal(body, ContentMarkup.Serialize(parsed));
        Assert.True(ContentMarkup.IsSafeHref($"https://example.com/hero.jpg"));
        Assert.True(ContentMarkup.IsSafeHref($"/hub/{businessId:D}/public-guide"));
        Assert.False(ContentMarkup.IsSafeHref("http://example.com/hero.jpg"));
        Assert.False(ContentMarkup.IsSafeHref("https://127.0.0.1/hero.jpg"));
        Assert.True(ContentMarkup.TryVideoEmbed("https://www.youtube.com/watch?v=abcdefghijk", out var youtube, out var provider));
        Assert.Equal("YouTube", provider);
        Assert.Equal("https://www.youtube-nocookie.com/embed/abcdefghijk", youtube);
        Assert.True(ContentMarkup.TryVideoEmbed("https://vimeo.com/123456789", out var vimeo, out var vimeoProvider));
        Assert.Equal("Vimeo", vimeoProvider);
        Assert.Equal("https://player.vimeo.com/video/123456789", vimeo);
        Assert.False(ContentMarkup.TryVideoEmbed("https://example.com/watch", out _, out _));
        var seo = ContentSeo.Evaluate(
            "How to choose a conference room system",
            "A practical excerpt for the hub checklist snippet.",
            "# How\n\nA conference room system should match the stored service and keep sentences readable for the checklist.",
            "conference",
            "https://example.com/guide",
            "how-to-choose",
            "Conference room systems",
            "A stored meta description that is long enough for a search snippet on this business.");
        Assert.Equal("Conference room systems", seo.MetaTitle);
        Assert.StartsWith("A stored meta description", seo.MetaDescription);
    }
}
