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
    bool ContainsEmail,
    bool ContainsAddress,
    bool ContainsVision,
    bool HasContactForm,
    string PageRole,
    Guid? AuditRunId,
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

public sealed record SearchConsoleQueryResponse(
    string Query,
    double Clicks,
    double Impressions,
    double Ctr,
    double Position);

public sealed record TestReportResponse(
    Guid Id,
    string Kind,
    string Title,
    string ObservedFact,
    string Recommendation,
    string HoldReason,
    Guid? AuditRunId,
    DateTimeOffset CreatedAtUtc);

public sealed record WebsiteIntelligenceResponse(
    WebsiteSnapshotResponse? Snapshot,
    IReadOnlyList<WebsiteSnapshotResponse> Pages,
    IReadOnlyList<SearchObservationResponse> Observations,
    SearchConsoleStatusResponse SearchConsole,
    IReadOnlyList<SearchConsoleQueryResponse> SearchConsoleQueries,
    IReadOnlyList<TestReportResponse> Reports,
    string SearchProvider,
    bool VectorSearchConfigured);

public sealed record SiteSearchHitResponse(string Title, string Url, string Snippet, double Score);

public sealed record SiteSearchResponse(
    string Provider,
    string Query,
    IReadOnlyList<SiteSearchHitResponse> Hits);
