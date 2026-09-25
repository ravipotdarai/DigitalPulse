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
    Visibility = 4,
    Page = 5,
    Vision = 6,
    Contact = 7
}

public enum WebsitePageRole
{
    Home = 1,
    About = 2,
    Contact = 3,
    Other = 4
}

public enum TestReportKind
{
    WebsiteAudit = 1,
    DigitalPulseCheck = 2,
    SearchConsole = 3,
    GoogleAds = 4,
    GoogleAnalytics = 5
}

public sealed record WebsitePageContext(
    WebsitePageRole Role = WebsitePageRole.Home,
    Guid? AuditRunId = null,
    bool ContainsEmail = false,
    bool ContainsAddress = false,
    bool ContainsVision = false,
    bool HasContactForm = false);

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
    public bool ContainsEmail { get; private set; }
    public bool ContainsAddress { get; private set; }
    public bool ContainsVision { get; private set; }
    public bool HasContactForm { get; private set; }
    public WebsitePageRole PageRole { get; private set; } = WebsitePageRole.Home;
    public Guid? AuditRunId { get; private set; }
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
        string? error,
        WebsitePageContext? page = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        var extras = page ?? new WebsitePageContext();

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
            ContainsEmail = extras.ContainsEmail,
            ContainsAddress = extras.ContainsAddress,
            ContainsVision = extras.ContainsVision,
            HasContactForm = extras.HasContactForm,
            PageRole = extras.Role,
            AuditRunId = extras.AuditRunId,
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

public sealed class SearchConsoleQuery : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public Guid AuditRunId { get; private set; }
    public string Query { get; private set; } = string.Empty;
    public double Clicks { get; private set; }
    public double Impressions { get; private set; }
    public double Ctr { get; private set; }
    public double Position { get; private set; }

    private SearchConsoleQuery() { }

    public static SearchConsoleQuery Record(
        Guid tenantId,
        Guid businessId,
        Guid auditRunId,
        string query,
        double clicks,
        double impressions,
        double ctr,
        double position)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        if (auditRunId == Guid.Empty) throw new ArgumentException("Audit run is required.", nameof(auditRunId));
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        return new SearchConsoleQuery
        {
            TenantId = tenantId,
            BusinessId = businessId,
            AuditRunId = auditRunId,
            Query = query.Trim(),
            Clicks = Math.Max(0, clicks),
            Impressions = Math.Max(0, impressions),
            Ctr = Math.Clamp(ctr, 0, 1),
            Position = Math.Max(0, position)
        };
    }
}

public sealed class TestReport : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public TestReportKind Kind { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string ObservedFact { get; private set; } = string.Empty;
    public string Recommendation { get; private set; } = string.Empty;
    public string HoldReason { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public Guid? AuditRunId { get; private set; }

    private TestReport() { }

    public static TestReport Assemble(
        Guid tenantId,
        Guid businessId,
        TestReportKind kind,
        string title,
        string observedFact,
        string recommendation,
        string holdReason,
        string body,
        Guid? auditRunId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(observedFact);

        return new TestReport
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Kind = kind,
            Title = title.Trim(),
            ObservedFact = observedFact.Trim(),
            Recommendation = recommendation.Trim(),
            HoldReason = holdReason.Trim(),
            Body = body.Trim(),
            AuditRunId = auditRunId
        };
    }
}
