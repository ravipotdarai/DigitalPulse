namespace DigitalPulse.Contracts.Website;

public sealed record WebsiteSnapshotResponse(
    Guid Id,
    Guid BusinessId,
    string? Url,
    string Status,
    int? StatusCode,
    string? Title,
    string? MetaDescription,
    string? H1,
    string? CanonicalUrl,
    string? Robots,
    bool HasJsonLd,
    bool HasFaqSchema,
    bool HasOrganizationSchema,
    bool HasOgTitle,
    int WordCount,
    bool ContainsBusinessName,
    bool ContainsPhone,
    string? Error,
    DateTimeOffset FetchedAtUtc);

public sealed record SearchObservationResponse(
    Guid Id,
    string Category,
    string Severity,
    string Title,
    string Detail,
    string? ExpectedValue,
    string? ObservedValue,
    string Recommendation);

public sealed record SearchConsoleStatusResponse(
    string Status,
    string? GrantKind,
    string Detail);

public sealed record WebsiteIntelligenceResponse(
    WebsiteSnapshotResponse? Snapshot,
    IReadOnlyList<SearchObservationResponse> Observations,
    SearchConsoleStatusResponse SearchConsole,
    string SearchProvider,
    bool VectorSearchConfigured);

public sealed record SiteSearchHitResponse(string Title, string Url, string Snippet, double Score);

public sealed record SiteSearchResponse(
    string Provider,
    string Query,
    IReadOnlyList<SiteSearchHitResponse> Hits);
