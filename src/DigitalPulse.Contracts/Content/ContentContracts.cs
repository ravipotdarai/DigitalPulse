namespace DigitalPulse.Contracts.Content;

public sealed record ContentTypeResponse(string Code, string Name);

public sealed record ContentSeoResponse(
    string SearchIntent,
    int ChecksPassed,
    int ChecksTotal,
    int SeoScore,
    string MetaTitle,
    string MetaDescription,
    string? FocusKeyword,
    string? CanonicalUrl,
    IReadOnlyList<string> Notes,
    DateTimeOffset? LastAnalyzedAtUtc,
    int ReadabilityScore,
    int AeoScore,
    int SlugScore,
    int InternalLinkScore,
    int EntityCoverageScore);

public sealed record ContentNamedResponse(Guid Id, string Name, string Slug);

public sealed record ContentRevisionResponse(Guid Id, int VersionNumber, string Title, string ChangeSummary, DateTimeOffset CreatedAtUtc);

public sealed record ContentVariantHubResponse(Guid Id, string Kind, string Title, string Body, string Status, string PublicationHold);

public sealed record ContentDistributionResponse(Guid Id, string ProviderCode, string Status, string? FailureReason, DateTimeOffset? PublishedAtUtc);

public sealed record ContentMetricResponse(string ProviderCode, DateOnly MetricDate, long? Views, string Detail);

public sealed record ContentMediaResponse(Guid Id, Guid MediaAssetId, string Role, int DisplayOrder, string? Label, string? SourceUrl);

public sealed record HubContentResponse(
    Guid Id,
    Guid BusinessId,
    Guid? ProjectId,
    string ContentTypeCode,
    string Title,
    string Slug,
    string Excerpt,
    string Body,
    string Status,
    string Visibility,
    string SourceNote,
    Guid? FeaturedMediaAssetId,
    string? CanonicalUrl,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset? ScheduledAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Tags,
    ContentSeoResponse Seo,
    IReadOnlyList<ContentRevisionResponse> Revisions,
    IReadOnlyList<ContentVariantHubResponse> Variants,
    IReadOnlyList<ContentDistributionResponse> Distributions,
    IReadOnlyList<ContentMetricResponse> Metrics,
    IReadOnlyList<ContentMediaResponse> Media);

public sealed record HubContentSummary(
    Guid Id,
    string ContentTypeCode,
    string Title,
    string Slug,
    string Status,
    string Visibility,
    DateTimeOffset UpdatedAtUtc,
    int SeoScore,
    string Excerpt,
    DateTimeOffset? PublishedAtUtc);

public sealed record ContentOpportunityResponse(
    Guid Id,
    Guid TopicId,
    string Topic,
    string Description,
    string SourceType,
    int? CoverageScore,
    int? OpportunityScore,
    int? RelevanceScore,
    int? CompetitionScore,
    int Priority,
    string Reason,
    string Status);

public sealed record ContentCalendarResponse(Guid Id, Guid ContentItemId, string Title, DateTimeOffset ScheduledAtUtc, string Status, string Channel);

public sealed record HubMediaAssetResponse(Guid Id, string Label, string Kind, string? SourceUrl);

public sealed record ContentHubWorkspace(
    IReadOnlyList<ContentTypeResponse> Types,
    IReadOnlyList<HubContentSummary> Items,
    IReadOnlyList<ContentOpportunityResponse> Opportunities,
    IReadOnlyList<ContentCalendarResponse> Calendar,
    IReadOnlyList<ContentNamedResponse> Categories,
    IReadOnlyList<ContentNamedResponse> Tags,
    IReadOnlyList<ContentMetricResponse> Metrics,
    IReadOnlyList<HubMediaAssetResponse> Media,
    string Note);

public sealed record CreateHubContentRequest(
    string ContentTypeCode,
    string Title,
    string? Slug,
    string Excerpt,
    string Body,
    string Visibility,
    Guid? ProjectId,
    string? FocusKeyword,
    string? CanonicalUrl,
    IReadOnlyList<string>? Categories,
    IReadOnlyList<string>? Tags,
    Guid? FeaturedMediaAssetId = null);

public sealed record UpdateHubContentRequest(
    string ContentTypeCode,
    string Title,
    string? Slug,
    string Excerpt,
    string Body,
    string Visibility,
    Guid? ProjectId,
    string? FocusKeyword,
    string? CanonicalUrl,
    string? ChangeSummary,
    IReadOnlyList<string>? Categories,
    IReadOnlyList<string>? Tags,
    Guid? FeaturedMediaAssetId = null);

public sealed record ScheduleHubContentRequest(DateTimeOffset ScheduledAtUtc, string? Channel);
public sealed record GenerateHubContentRequest(string? OpportunityId, string Prompt);
public sealed record AnalyzeHubSeoRequest(string? FocusKeyword);
public sealed record DistributeHubContentRequest(string ProviderCode);
public sealed record DiscoverOpportunitiesRequest();
public sealed record RejectHubContentRequest(string? Note);
public sealed record AttachHubMediaRequest(Guid MediaAssetId, string Role);
public sealed record RegisterHubMediaRequest(string Label, string Kind, string SourceUrl);
public sealed record PublicHubArticle(
    string BusinessName,
    string Title,
    string Slug,
    string Excerpt,
    string Body,
    string ContentTypeCode,
    DateTimeOffset PublishedAtUtc,
    string? FeaturedImageUrl);
public sealed record PublicHubArticleSummary(
    string Title,
    string Slug,
    string Excerpt,
    string ContentTypeCode,
    DateTimeOffset PublishedAtUtc,
    string? FeaturedImageUrl);
public sealed record PublicHubIndex(Guid BusinessId, string BusinessName, IReadOnlyList<PublicHubArticleSummary> Articles);
