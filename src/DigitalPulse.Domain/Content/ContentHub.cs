using DigitalPulse.Domain.Common;
using DigitalPulse.Domain.Safety;

namespace DigitalPulse.Domain.Content;

public enum ContentMediaRole
{
    Featured = 1,
    Body = 2,
    Gallery = 3,
    Thumbnail = 4,
    Social = 5
}

public enum ContentOpportunitySource
{
    Manual = 1,
    Service = 2,
    Project = 3,
    Finding = 4,
    WebsiteGap = 5,
    CustomerQuestion = 6,
    Seasonal = 7,
    Ai = 8,
    Search = 9,
    Competitor = 10
}

public enum ContentOpportunityStatus
{
    New = 1,
    Accepted = 2,
    Dismissed = 3,
    Converted = 4
}

public enum ContentDistributionStatus
{
    Draft = 1,
    ApprovalRequired = 2,
    Approved = 3,
    Scheduled = 4,
    Publishing = 5,
    Published = 6,
    Failed = 7,
    Cancelled = 8
}

public sealed class ContentCategory : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private ContentCategory() { }

    public static ContentCategory Create(Guid tenantId, Guid businessId, string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ContentSafety.EnsureAllowed(name, description);
        return new ContentCategory
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Name = name.Trim(),
            Slug = ContentSlug.From(null, name),
            Description = description?.Trim() ?? string.Empty
        };
    }
}

public sealed class ContentTag : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;

    private ContentTag() { }

    public static ContentTag Create(Guid tenantId, Guid businessId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ContentSafety.EnsureAllowed(name);
        return new ContentTag
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Name = name.Trim(),
            Slug = ContentSlug.From(null, name)
        };
    }
}

public sealed class ContentItemCategory : TenantOwnedEntity
{
    public Guid ContentItemId { get; private set; }
    public Guid ContentCategoryId { get; private set; }

    private ContentItemCategory() { }

    public static ContentItemCategory Link(Guid tenantId, Guid contentItemId, Guid categoryId) =>
        new()
        {
            TenantId = tenantId,
            ContentItemId = contentItemId,
            ContentCategoryId = categoryId
        };
}

public sealed class ContentItemTag : TenantOwnedEntity
{
    public Guid ContentItemId { get; private set; }
    public Guid ContentTagId { get; private set; }

    private ContentItemTag() { }

    public static ContentItemTag Link(Guid tenantId, Guid contentItemId, Guid tagId) =>
        new()
        {
            TenantId = tenantId,
            ContentItemId = contentItemId,
            ContentTagId = tagId
        };
}

public sealed class ContentRevision : TenantOwnedEntity
{
    public Guid ContentItemId { get; private set; }
    public int VersionNumber { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Excerpt { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string ChangeSummary { get; private set; } = string.Empty;
    public Guid? CreatedByUserId { get; private set; }

    private ContentRevision() { }

    public static ContentRevision Capture(
        Guid tenantId,
        Guid contentItemId,
        int versionNumber,
        string title,
        string excerpt,
        string body,
        string changeSummary,
        Guid? createdByUserId) =>
        new()
        {
            TenantId = tenantId,
            ContentItemId = contentItemId,
            VersionNumber = versionNumber,
            Title = title,
            Excerpt = excerpt,
            Body = body,
            ChangeSummary = changeSummary.Trim(),
            CreatedByUserId = createdByUserId
        };
}

public sealed class ContentItemMedia : TenantOwnedEntity
{
    public Guid ContentItemId { get; private set; }
    public Guid MediaAssetId { get; private set; }
    public int DisplayOrder { get; private set; }
    public ContentMediaRole Role { get; private set; }

    private ContentItemMedia() { }

    public static ContentItemMedia Attach(
        Guid tenantId,
        Guid contentItemId,
        Guid mediaAssetId,
        ContentMediaRole role,
        int displayOrder) =>
        new()
        {
            TenantId = tenantId,
            ContentItemId = contentItemId,
            MediaAssetId = mediaAssetId,
            Role = role,
            DisplayOrder = displayOrder
        };
}

public sealed class ContentSeoAnalysis : TenantOwnedEntity
{
    public Guid ContentItemId { get; private set; }
    public string? FocusKeyword { get; private set; }
    public string SearchIntent { get; private set; } = "Unknown";
    public int ChecksPassed { get; private set; }
    public int ChecksTotal { get; private set; }
    public int SeoScore { get; private set; }
    public int ReadabilityScore { get; private set; }
    public int AeoScore { get; private set; }
    public int SlugScore { get; private set; }
    public int InternalLinkScore { get; private set; }
    public int EntityCoverageScore { get; private set; }
    public string MetaTitle { get; private set; } = string.Empty;
    public string MetaDescription { get; private set; } = string.Empty;
    public string? CanonicalUrl { get; private set; }
    public string NotesJson { get; private set; } = "[]";
    public string ChecksJson { get; private set; } = "{}";
    public DateTimeOffset LastAnalyzedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private ContentSeoAnalysis() { }

    public static ContentSeoAnalysis Capture(
        Guid tenantId,
        Guid contentItemId,
        string? focusKeyword,
        string searchIntent,
        int passed,
        int total,
        string metaTitle,
        string metaDescription,
        string? canonicalUrl,
        string notesJson,
        int readabilityScore,
        int aeoScore,
        int slugScore,
        int internalLinkScore,
        int entityCoverageScore,
        string? checksJson = null) =>
        new()
        {
            TenantId = tenantId,
            ContentItemId = contentItemId,
            FocusKeyword = string.IsNullOrWhiteSpace(focusKeyword) ? null : focusKeyword.Trim(),
            SearchIntent = searchIntent,
            ChecksPassed = passed,
            ChecksTotal = total,
            SeoScore = total == 0 ? 0 : (int)Math.Round(100d * passed / total),
            ReadabilityScore = ClampScore(readabilityScore),
            AeoScore = ClampScore(aeoScore),
            SlugScore = ClampScore(slugScore),
            InternalLinkScore = ClampScore(internalLinkScore),
            EntityCoverageScore = ClampScore(entityCoverageScore),
            MetaTitle = metaTitle,
            MetaDescription = metaDescription,
            CanonicalUrl = canonicalUrl,
            NotesJson = notesJson,
            ChecksJson = string.IsNullOrWhiteSpace(checksJson) ? "{}" : checksJson,
            LastAnalyzedAtUtc = DateTimeOffset.UtcNow
        };

    public void Replace(ContentSeoAnalysis next)
    {
        FocusKeyword = next.FocusKeyword;
        SearchIntent = next.SearchIntent;
        ChecksPassed = next.ChecksPassed;
        ChecksTotal = next.ChecksTotal;
        SeoScore = next.SeoScore;
        ReadabilityScore = next.ReadabilityScore;
        AeoScore = next.AeoScore;
        SlugScore = next.SlugScore;
        InternalLinkScore = next.InternalLinkScore;
        EntityCoverageScore = next.EntityCoverageScore;
        MetaTitle = next.MetaTitle;
        MetaDescription = next.MetaDescription;
        CanonicalUrl = next.CanonicalUrl;
        NotesJson = next.NotesJson;
        ChecksJson = next.ChecksJson;
        LastAnalyzedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    private static int ClampScore(int value) => Math.Clamp(value, 0, 100);
}

public sealed class ContentTopic : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public string Topic { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string SearchIntent { get; private set; } = "Informational";
    public string Status { get; private set; } = "Open";

    private ContentTopic() { }

    public static ContentTopic Create(Guid tenantId, Guid businessId, string topic, string description, string searchIntent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ContentSafety.EnsureAllowed(topic, description);
        return new ContentTopic
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Topic = topic.Trim(),
            Description = description.Trim(),
            SearchIntent = string.IsNullOrWhiteSpace(searchIntent) ? "Informational" : searchIntent.Trim()
        };
    }
}

public sealed class ContentOpportunity : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public Guid ContentTopicId { get; private set; }
    public ContentOpportunitySource SourceType { get; private set; }
    public int? RelevanceScore { get; private set; }
    public int? OpportunityScore { get; private set; }
    public int? CompetitionScore { get; private set; }
    public int? CoverageScore { get; private set; }
    public int Priority { get; private set; } = 5;
    public string Reason { get; private set; } = string.Empty;
    public ContentOpportunityStatus Status { get; private set; } = ContentOpportunityStatus.New;

    private ContentOpportunity() { }

    public static ContentOpportunity Open(
        Guid tenantId,
        Guid businessId,
        Guid topicId,
        ContentOpportunitySource source,
        int coverageScore,
        string reason) =>
        new()
        {
            TenantId = tenantId,
            BusinessId = businessId,
            ContentTopicId = topicId,
            SourceType = source,
            CoverageScore = coverageScore,
            OpportunityScore = 100 - coverageScore,
            RelevanceScore = RelevanceFor(source),
            CompetitionScore = null,
            Priority = PriorityFor(100 - coverageScore),
            Reason = reason.Trim(),
            Status = ContentOpportunityStatus.New
        };

    private static int RelevanceFor(ContentOpportunitySource source) => source switch
    {
        ContentOpportunitySource.Service => 100,
        ContentOpportunitySource.Project => 90,
        ContentOpportunitySource.Finding => 70,
        ContentOpportunitySource.WebsiteGap => 60,
        ContentOpportunitySource.CustomerQuestion => 55,
        ContentOpportunitySource.Seasonal => 50,
        _ => 40
    };

    private static int PriorityFor(int opportunityScore) => opportunityScore switch
    {
        >= 80 => 1,
        >= 60 => 2,
        >= 40 => 3,
        >= 20 => 4,
        _ => 5
    };

    public void Accept()
    {
        Status = ContentOpportunityStatus.Accepted;
        Touch();
    }

    public void Dismiss()
    {
        Status = ContentOpportunityStatus.Dismissed;
        Touch();
    }

    public void Convert()
    {
        Status = ContentOpportunityStatus.Converted;
        Touch();
    }
}

public sealed class ContentCalendarEntry : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public Guid ContentItemId { get; private set; }
    public Guid? ContentVariantId { get; private set; }
    public DateTimeOffset ScheduledAtUtc { get; private set; }
    public string Status { get; private set; } = "Scheduled";
    public string Channel { get; private set; } = "HUB";
    public Guid? CreatedByUserId { get; private set; }

    private ContentCalendarEntry() { }

    public static ContentCalendarEntry Schedule(
        Guid tenantId,
        Guid businessId,
        Guid contentItemId,
        DateTimeOffset atUtc,
        string channel,
        Guid? variantId,
        Guid? userId) =>
        new()
        {
            TenantId = tenantId,
            BusinessId = businessId,
            ContentItemId = contentItemId,
            ContentVariantId = variantId,
            ScheduledAtUtc = atUtc,
            Status = "Scheduled",
            Channel = string.IsNullOrWhiteSpace(channel) ? "HUB" : channel.Trim().ToUpperInvariant(),
            CreatedByUserId = userId
        };

    public void MarkPublished()
    {
        Status = "Published";
        Touch();
    }

    public void Cancel()
    {
        Status = "Cancelled";
        Touch();
    }
}

public sealed class ContentDistribution : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public Guid ContentItemId { get; private set; }
    public Guid? ContentVariantId { get; private set; }
    public string ProviderCode { get; private set; } = string.Empty;
    public Guid? PlatformConnectionId { get; private set; }
    public ContentDistributionStatus Status { get; private set; } = ContentDistributionStatus.Draft;
    public DateTimeOffset? ScheduledAtUtc { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public string? ExternalContentId { get; private set; }
    public string? FailureReason { get; private set; }

    private ContentDistribution() { }

    public static ContentDistribution Start(
        Guid tenantId,
        Guid businessId,
        Guid contentItemId,
        string providerCode,
        Guid? variantId,
        Guid? connectionId) =>
        new()
        {
            TenantId = tenantId,
            BusinessId = businessId,
            ContentItemId = contentItemId,
            ContentVariantId = variantId,
            ProviderCode = providerCode.Trim().ToUpperInvariant(),
            PlatformConnectionId = connectionId,
            Status = ContentDistributionStatus.ApprovalRequired
        };

    public void Hold(string reason)
    {
        Status = ContentDistributionStatus.Failed;
        FailureReason = reason.Trim();
        Touch();
    }

    public void MarkPublished(string? externalId)
    {
        Status = ContentDistributionStatus.Published;
        PublishedAtUtc = DateTimeOffset.UtcNow;
        ExternalContentId = externalId;
        FailureReason = null;
        Touch();
    }
}

public sealed class ContentMetric : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public Guid ContentItemId { get; private set; }
    public Guid? ContentVariantId { get; private set; }
    public string ProviderCode { get; private set; } = string.Empty;
    public DateOnly MetricDate { get; private set; }
    public long? Views { get; private set; }
    public long? Clicks { get; private set; }
    public long? Engagements { get; private set; }
    public long? Shares { get; private set; }
    public long? Reactions { get; private set; }
    public long? Comments { get; private set; }
    public long? Leads { get; private set; }
    public long? Conversions { get; private set; }
    public string Detail { get; private set; } = string.Empty;

    private ContentMetric() { }

    public static ContentMetric Observed(
        Guid tenantId,
        Guid businessId,
        Guid contentItemId,
        string providerCode,
        DateOnly date,
        long views,
        string detail) =>
        new()
        {
            TenantId = tenantId,
            BusinessId = businessId,
            ContentItemId = contentItemId,
            ProviderCode = providerCode.Trim().ToUpperInvariant(),
            MetricDate = date,
            Views = views,
            Detail = detail.Trim()
        };

    public static ContentMetric Hold(
        Guid tenantId,
        Guid businessId,
        Guid contentItemId,
        string detail) =>
        new()
        {
            TenantId = tenantId,
            BusinessId = businessId,
            ContentItemId = contentItemId,
            ProviderCode = "NONE",
            MetricDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Detail = detail.Trim()
        };
}
