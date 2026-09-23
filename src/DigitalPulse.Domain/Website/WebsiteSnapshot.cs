using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Website;

public enum WebsiteFetchStatus
{
    Reached = 1,
    Unreachable = 2,
    Blocked = 3,
    Skipped = 4
}

public enum SearchObservationCategory
{
    Seo = 1,
    Aeo = 2,
    SearchConsole = 3,
    Visibility = 4
}

public enum SearchObservationSeverity
{
    Info = 0,
    Low = 1,
    Medium = 2,
    High = 3
}

public sealed class WebsiteSnapshot : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public string? Url { get; private set; }
    public WebsiteFetchStatus Status { get; private set; }
    public int? StatusCode { get; private set; }
    public string? Title { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? H1 { get; private set; }
    public string? CanonicalUrl { get; private set; }
    public string? Robots { get; private set; }
    public bool HasJsonLd { get; private set; }
    public bool HasFaqSchema { get; private set; }
    public bool HasOrganizationSchema { get; private set; }
    public bool HasOgTitle { get; private set; }
    public int WordCount { get; private set; }
    public bool ContainsBusinessName { get; private set; }
    public bool ContainsPhone { get; private set; }
    public string? Error { get; private set; }
    public DateTimeOffset FetchedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private WebsiteSnapshot() { }

    public static WebsiteSnapshot Record(
        Guid tenantId,
        Guid businessId,
        string? url,
        WebsiteFetchStatus status,
        int? statusCode,
        string? title,
        string? metaDescription,
        string? h1,
        string? canonicalUrl,
        string? robots,
        bool hasJsonLd,
        bool hasFaqSchema,
        bool hasOrganizationSchema,
        bool hasOgTitle,
        int wordCount,
        bool containsBusinessName,
        bool containsPhone,
        string? error)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));

        return new WebsiteSnapshot
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Url = NullIfEmpty(url),
            Status = status,
            StatusCode = statusCode,
            Title = NullIfEmpty(title),
            MetaDescription = NullIfEmpty(metaDescription),
            H1 = NullIfEmpty(h1),
            CanonicalUrl = NullIfEmpty(canonicalUrl),
            Robots = NullIfEmpty(robots),
            HasJsonLd = hasJsonLd,
            HasFaqSchema = hasFaqSchema,
            HasOrganizationSchema = hasOrganizationSchema,
            HasOgTitle = hasOgTitle,
            WordCount = Math.Max(0, wordCount),
            ContainsBusinessName = containsBusinessName,
            ContainsPhone = containsPhone,
            Error = NullIfEmpty(error),
            FetchedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class SearchObservation : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public Guid SnapshotId { get; private set; }
    public SearchObservationCategory Category { get; private set; }
    public SearchObservationSeverity Severity { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Detail { get; private set; } = string.Empty;
    public string? ExpectedValue { get; private set; }
    public string? ObservedValue { get; private set; }
    public string Recommendation { get; private set; } = string.Empty;

    private SearchObservation() { }

    public static SearchObservation Create(
        Guid tenantId,
        Guid businessId,
        Guid snapshotId,
        SearchObservationCategory category,
        SearchObservationSeverity severity,
        string title,
        string detail,
        string? expectedValue,
        string? observedValue,
        string recommendation)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        if (snapshotId == Guid.Empty) throw new ArgumentException("Snapshot is required.", nameof(snapshotId));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);

        return new SearchObservation
        {
            TenantId = tenantId,
            BusinessId = businessId,
            SnapshotId = snapshotId,
            Category = category,
            Severity = severity,
            Title = title.Trim(),
            Detail = detail.Trim(),
            ExpectedValue = string.IsNullOrWhiteSpace(expectedValue) ? null : expectedValue.Trim(),
            ObservedValue = string.IsNullOrWhiteSpace(observedValue) ? null : observedValue.Trim(),
            Recommendation = recommendation.Trim()
        };
    }
}
