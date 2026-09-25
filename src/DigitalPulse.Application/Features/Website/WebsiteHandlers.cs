using System.Net.Http;
using System.Text.Json;
using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Identity;
using DigitalPulse.Application.Website;
using DigitalPulse.Contracts.Website;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Platforms;
using DigitalPulse.Domain.Website;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Website;

internal static class WebsiteMap
{
    public static WebsiteSnapshotResponse ToResponse(this WebsiteSnapshot snapshot) =>
        new(
            snapshot.Id,
            snapshot.BusinessId,
            snapshot.Url,
            snapshot.Status.ToString(),
            snapshot.StatusCode,
            snapshot.Title,
            snapshot.MetaDescription,
            snapshot.H1,
            snapshot.CanonicalUrl,
            snapshot.Robots,
            snapshot.HasJsonLd,
            snapshot.HasFaqSchema,
            snapshot.HasOrganizationSchema,
            snapshot.HasOgTitle,
            snapshot.WordCount,
            snapshot.ContainsBusinessName,
            snapshot.ContainsPhone,
            snapshot.ContainsEmail,
            snapshot.ContainsAddress,
            snapshot.ContainsVision,
            snapshot.HasContactForm,
            snapshot.PageRole.ToString(),
            snapshot.AuditRunId,
            snapshot.Error,
            snapshot.FetchedAtUtc);

    public static SearchObservationResponse ToResponse(this SearchObservation observation) =>
        new(
            observation.Id,
            observation.Category.ToString(),
            observation.Severity.ToString(),
            observation.Title,
            observation.Detail,
            observation.ExpectedValue,
            observation.ObservedValue,
            observation.Recommendation);

    public static TestReportResponse ToResponse(this TestReport report) =>
        new(
            report.Id,
            report.Kind.ToString(),
            report.Title,
            report.ObservedFact,
            report.Recommendation,
            report.HoldReason,
            report.AuditRunId,
            report.CreatedAtUtc);

    public static SearchConsoleQueryResponse ToResponse(this SearchConsoleQuery row) =>
        new(row.Query, row.Clicks, row.Impressions, row.Ctr, row.Position);

    public static string NormalizeSite(string? website)
    {
        var site = (website ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(site))
        {
            return string.Empty;
        }

        if (!site.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !site.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            site = "https://" + site;
        }

        return site.EndsWith('/') ? site : site + "/";
    }

    public static async Task<SearchConsoleStatusResponse> ConsoleStatusAsync(
        PlatformConnection? connection,
        string? website,
        IOfficialPlatformGateway gateway,
        CancellationToken cancellationToken)
    {
        if (connection is null)
        {
            return new("NotConnected", null, "Search Console is not connected. Impressions and queries are not invented.");
        }

        if (connection.Status == ConnectionStatus.NeedsReauth)
        {
            return new("NeedsReauth", connection.GrantKind, connection.LastError ?? "Search Console needs reauthorization. Impressions were not invented.");
        }

        if (connection.Status != ConnectionStatus.Connected)
        {
            return new("NotConnected", connection.GrantKind, "Search Console is not connected. Impressions and queries are not invented.");
        }

        if (!connection.HasLiveCredential)
        {
            return new(
                "Hold",
                connection.GrantKind,
                string.Equals(connection.GrantKind, "Development", StringComparison.OrdinalIgnoreCase)
                    ? "Development grant only. Search Console coverage is not invented."
                    : "Search Console is connected without a live OAuth token. Coverage is not invented.");
        }

        var site = NormalizeSite(website);
        if (string.IsNullOrWhiteSpace(site))
        {
            return new("Hold", connection.GrantKind, "Add the official website on the identity record before Search Console metrics can be queried.");
        }

        var end = DateTime.UtcNow.Date;
        var start = end.AddDays(-7);
        var body = JsonSerializer.Serialize(new
        {
            startDate = start.ToString("yyyy-MM-dd"),
            endDate = end.ToString("yyyy-MM-dd"),
            dimensions = new[] { "query" },
            rowLimit = 10
        });
        var url = $"https://searchconsole.googleapis.com/webmasters/v3/sites/{Uri.EscapeDataString(site)}/searchAnalytics/query";
        var result = await gateway.SendAsync(HttpMethod.Post, url, connection.AccessToken, body, null, cancellationToken);
        if (result.Ok)
        {
            return new("Observed", connection.GrantKind, result.Body);
        }

        return new(
            result.StatusCode is 401 or 403 ? "NeedsReauth" : "Hold",
            connection.GrantKind,
            $"Official Search Console returned {result.StatusCode}. Impressions were not invented.");
    }
}

internal static class WebsiteIntelligenceLoader
{
    public static async Task<WebsiteIntelligenceResponse> LoadAsync(
        IAppDbContext db,
        ISearchProvider search,
        IVectorSearchProvider vectors,
        Guid businessId,
        SearchConsoleStatusResponse searchConsole,
        CancellationToken cancellationToken)
    {
        var latest = await db.WebsiteSnapshots.AsNoTracking()
            .Where(s => s.BusinessId == businessId)
            .OrderByDescending(s => s.FetchedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var runId = latest?.AuditRunId;
        var pages = runId is null
            ? latest is null ? [] : new List<WebsiteSnapshot> { latest }
            : await db.WebsiteSnapshots.AsNoTracking()
                .Where(s => s.BusinessId == businessId && s.AuditRunId == runId)
                .OrderBy(s => s.PageRole)
                .ThenBy(s => s.Url)
                .ToListAsync(cancellationToken);
        var snapshotIds = pages.Select(p => p.Id).ToList();
        var observations = snapshotIds.Count == 0
            ? []
            : await db.SearchObservations.AsNoTracking()
                .Where(o => snapshotIds.Contains(o.SnapshotId))
                .OrderByDescending(o => o.Severity)
                .ThenBy(o => o.Title)
                .ToListAsync(cancellationToken);
        var queries = runId is null
            ? []
            : await db.SearchConsoleQueries.AsNoTracking()
                .Where(q => q.BusinessId == businessId && q.AuditRunId == runId)
                .OrderByDescending(q => q.Impressions)
                .ToListAsync(cancellationToken);
        var reports = await db.TestReports.AsNoTracking()
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(12)
            .ToListAsync(cancellationToken);
        var home = pages.FirstOrDefault(p => p.PageRole == WebsitePageRole.Home) ?? pages.FirstOrDefault();

        return new WebsiteIntelligenceResponse(
            home?.ToResponse(),
            pages.Select(p => p.ToResponse()).ToList(),
            observations.Select(o => o.ToResponse()).ToList(),
            searchConsole,
            queries.Select(q => q.ToResponse()).ToList(),
            reports.Select(r => r.ToResponse()).ToList(),
            search.ProviderCode,
            vectors.IsConfigured);
    }
}

public sealed class GetWebsiteIntelligenceHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ISearchProvider _search;
    private readonly IVectorSearchProvider _vectors;

    public GetWebsiteIntelligenceHandler(
        IAppDbContext db,
        ITenantContext tenant,
        ISearchProvider search,
        IVectorSearchProvider vectors)
    {
        _db = db;
        _tenant = tenant;
        _search = search;
        _vectors = vectors;
    }

    public async Task<WebsiteIntelligenceResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var lastGsc = await _db.TestReports.AsNoTracking()
            .Where(r => r.BusinessId == businessId && r.Kind == TestReportKind.SearchConsole)
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var status = lastGsc is null
            ? new SearchConsoleStatusResponse("Hold", null, "Search Console has not been queried yet. Impressions are not invented.")
            : new SearchConsoleStatusResponse(
                string.IsNullOrWhiteSpace(lastGsc.HoldReason) ? "Observed" : "Hold",
                null,
                string.IsNullOrWhiteSpace(lastGsc.HoldReason) ? lastGsc.ObservedFact : lastGsc.HoldReason);
        return await WebsiteIntelligenceLoader.LoadAsync(_db, _search, _vectors, businessId, status, cancellationToken);
    }
}

public sealed class AnalyzeWebsiteHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IWebsiteFetcher _fetcher;
    private readonly ISearchProvider _search;
    private readonly IVectorSearchProvider _vectors;
    private readonly IOfficialPlatformGateway _gateway;
    private readonly IPlatformAdapterCatalog _catalog;
    private readonly ILiveTokenRefresher _tokens;

    public AnalyzeWebsiteHandler(
        IAppDbContext db,
        ITenantContext tenant,
        IWebsiteFetcher fetcher,
        ISearchProvider search,
        IVectorSearchProvider vectors,
        IOfficialPlatformGateway gateway,
        IPlatformAdapterCatalog catalog,
        ILiveTokenRefresher tokens)
    {
        _db = db;
        _tenant = tenant;
        _fetcher = fetcher;
        _search = search;
        _vectors = vectors;
        _gateway = gateway;
        _catalog = catalog;
        _tokens = tokens;
    }

    public async Task<WebsiteIntelligenceResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var business = await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        if (string.IsNullOrWhiteSpace(business.Website))
        {
            throw AppException.Validation("Add the official website on the identity record before running website analysis.");
        }

        var phones = await _db.ContactPoints.AsNoTracking()
            .Where(c => c.BusinessId == businessId && c.Kind == ContactPointKind.Phone)
            .ToListAsync(cancellationToken);
        var emails = await _db.ContactPoints.AsNoTracking()
            .Where(c => c.BusinessId == businessId && c.Kind == ContactPointKind.Email)
            .ToListAsync(cancellationToken);
        var contacts = await _db.ContactPoints.AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .ToListAsync(cancellationToken);
        var locations = await _db.Locations.AsNoTracking()
            .Where(l => l.BusinessId == businessId)
            .ToListAsync(cancellationToken);
        var facts = await _db.Facts.AsNoTracking()
            .Where(f => f.BusinessId == businessId)
            .ToListAsync(cancellationToken);
        var connections = await _db.Connections
            .Where(c => c.BusinessId == businessId)
            .ToListAsync(cancellationToken);
        foreach (var connection in connections)
        {
            await _tokens.EnsureFreshAsync(connection, cancellationToken);
        }
        var vision = facts.FirstOrDefault(f =>
            f.Status == FactStatus.Approved &&
            (f.FactTypeCode.Equals("VISION", StringComparison.OrdinalIgnoreCase)
             || f.FactTypeCode.Equals("MISSION", StringComparison.OrdinalIgnoreCase)))?.Value;
        var phone = phones.FirstOrDefault()?.Value;
        var email = emails.FirstOrDefault()?.Value;
        var location = locations.FirstOrDefault();

        var auditRunId = Guid.NewGuid();
        var crawled = await WebsiteSiteCrawler.CrawlAsync(_fetcher, business.Website, cancellationToken);
        var snapshots = new List<WebsiteSnapshot>();
        var observations = new List<SearchObservation>();

        foreach (var page in crawled)
        {
            var signals = page.Signals;
            var role = WebsitePageRoles.Classify(page.Url, signals?.Title, signals?.H1);
            var snapshot = WebsiteSnapshot.Record(
                tenantId,
                businessId,
                page.Fetch.FinalUrl ?? page.Url,
                StatusOf(page.Fetch),
                page.Fetch.StatusCode,
                signals?.Title,
                signals?.MetaDescription,
                signals?.H1,
                signals?.Canonical,
                signals?.Robots,
                signals?.HasJsonLd ?? false,
                signals?.HasFaqSchema ?? false,
                signals?.HasOrganizationSchema ?? false,
                signals?.HasOgTitle ?? false,
                signals?.WordCount ?? 0,
                HtmlSignalParser.ContainsName(signals?.Text, business.Name) || HtmlSignalParser.ContainsName(signals?.Title, business.Name),
                HtmlSignalParser.ContainsPhone(signals?.Text, phone),
                page.Fetch.Error,
                new WebsitePageContext(
                    role,
                    auditRunId,
                    HtmlSignalParser.ContainsEmail(signals?.Text, email),
                    HtmlSignalParser.ContainsAddress(signals?.Text, location?.AddressLine, location?.City),
                    HtmlSignalParser.ContainsVision(signals?.Text, vision),
                    signals?.HasContactForm ?? false));
            _db.WebsiteSnapshots.Add(snapshot);
            snapshots.Add(snapshot);

            if (page.Fetch.Reached && signals is not null)
            {
                var document = new SearchDocument(
                    tenantId,
                    businessId,
                    "Website",
                    signals.Title ?? business.Name,
                    signals.Text,
                    snapshot.Url ?? business.Website);
                await _search.IndexAsync(document, cancellationToken);
                if (_vectors.IsConfigured)
                {
                    await _vectors.IndexAsync(document, cancellationToken);
                }
            }
        }

        var gsc = connections.FirstOrDefault(c => c.PlatformCode.Equals("SEARCH_CONSOLE", StringComparison.OrdinalIgnoreCase));
        var console = await WebsiteMap.ConsoleStatusAsync(gsc, business.Website, _gateway, cancellationToken);
        IReadOnlyList<SearchConsoleQueryRow>? gscRows = null;
        if (console.Status == "Observed")
        {
            gscRows = SearchConsoleQueries.Parse(console.Detail);
            foreach (var row in gscRows)
            {
                _db.SearchConsoleQueries.Add(SearchConsoleQuery.Record(
                    tenantId, businessId, auditRunId, row.Query, row.Clicks, row.Impressions, row.Ctr, row.Position));
            }
        }

        var home = snapshots.FirstOrDefault(s => s.PageRole == WebsitePageRole.Home) ?? snapshots.First();
        var homeSignals = crawled.FirstOrDefault(p =>
            string.Equals(p.Url, home.Url, StringComparison.OrdinalIgnoreCase))?.Signals;
        var drafts = WebsiteObservations.FromSnapshot(business.Name, home, homeSignals, connections, gscRows)
            .Concat(WebsitePageAudits.FromPages(business, snapshots, contacts, locations, facts))
            .ToList();
        foreach (var draft in drafts)
        {
            var observation = SearchObservation.Create(
                tenantId,
                businessId,
                home.Id,
                draft.Category,
                draft.Severity,
                draft.Title,
                draft.Detail,
                draft.ExpectedValue,
                draft.ObservedValue,
                draft.Recommendation);
            _db.SearchObservations.Add(observation);
            observations.Add(observation);
        }

        _db.TestReports.Add(TestReport.Assemble(
            tenantId,
            businessId,
            TestReportKind.WebsiteAudit,
            "Website audit",
            $"{snapshots.Count} official page(s) crawled on the same host. Product descriptions were not scored.",
            drafts.Count == 0
                ? "No page gaps observed on this crawl."
                : "Fix About, Contact, vision, and on-page SEO from the identity record.",
            snapshots.Any(s => s.Status != WebsiteFetchStatus.Reached) ? snapshots.First(s => s.Status != WebsiteFetchStatus.Reached).Error ?? "A page did not fetch." : string.Empty,
            string.Join('\n', snapshots.Select(s => $"{s.PageRole}: {s.Url} ({s.Status})")),
            auditRunId));
        _db.TestReports.Add(TestReport.Assemble(
            tenantId,
            businessId,
            TestReportKind.SearchConsole,
            "Search Console",
            console.Status == "Observed"
                ? $"{gscRows?.Count ?? 0} official quer(y/ies) stored. Clicks were not invented."
                : console.Detail,
            console.Status == "Observed"
                ? "Improve matching pages on the website. Rankings cannot be written."
                : "Connect or reauthorize Search Console for a live grant.",
            console.Status == "Observed" ? string.Empty : console.Detail,
            console.Status == "Observed" ? (gscRows?.Count ?? 0).ToString() : console.Detail,
            auditRunId));

        await RecordAdapterReportAsync(tenantId, businessId, auditRunId, connections, "GOOGLE_ADS", TestReportKind.GoogleAds, "Google Ads", cancellationToken);
        await RecordAdapterReportAsync(tenantId, businessId, auditRunId, connections, "GOOGLE_ANALYTICS", TestReportKind.GoogleAnalytics, "Google Analytics", cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        var storedStatus = new SearchConsoleStatusResponse(
            console.Status,
            console.GrantKind,
            console.Status == "Observed"
                ? $"{gscRows?.Count ?? 0} official quer(y/ies) stored. Clicks were not invented."
                : console.Detail);
        return await WebsiteIntelligenceLoader.LoadAsync(_db, _search, _vectors, businessId, storedStatus, cancellationToken);
    }

    private async Task RecordAdapterReportAsync(
        Guid tenantId,
        Guid businessId,
        Guid auditRunId,
        IReadOnlyCollection<PlatformConnection> connections,
        string platform,
        TestReportKind kind,
        string title,
        CancellationToken cancellationToken)
    {
        var connection = connections.FirstOrDefault(c => c.PlatformCode.Equals(platform, StringComparison.OrdinalIgnoreCase));
        if (connection is null)
        {
            _db.TestReports.Add(TestReport.Assemble(
                tenantId, businessId, kind, title,
                $"{title} is not connected. Metrics were not invented.",
                $"Connect {title} in Connection Center.",
                "Not connected",
                "Not connected",
                auditRunId));
            return;
        }

        var result = await _catalog.Get(platform).MetricsAsync(connection, cancellationToken);
        var held = !result.Status.Equals("Observed", StringComparison.OrdinalIgnoreCase);
        var observed = result.Detail;
        var recommendation = held
            ? "Complete official login or configure the required token. Values are not invented."
            : "Review the official rows. Campaigns are not mutated.";
        if (!held && platform.Equals("GOOGLE_ADS", StringComparison.OrdinalIgnoreCase))
        {
            var campaigns = GoogleAdsCampaigns.Parse(result.Detail);
            observed = campaigns.Count == 0
                ? "Google returned no campaigns. Campaigns were not invented."
                : $"{campaigns.Count} official campaign(s) stored. Campaigns were not invented.";
        }

        _db.TestReports.Add(TestReport.Assemble(
            tenantId,
            businessId,
            kind,
            title,
            observed,
            recommendation,
            held ? result.Detail : string.Empty,
            result.Detail,
            auditRunId));
    }

    private static WebsiteFetchStatus StatusOf(WebsiteFetchResult fetch)
    {
        if (fetch.Disabled) return WebsiteFetchStatus.Skipped;
        if (fetch.Blocked) return WebsiteFetchStatus.Blocked;
        if (fetch.Reached) return WebsiteFetchStatus.Reached;
        return WebsiteFetchStatus.Unreachable;
    }
}

public sealed class SearchWebsiteHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ISearchProvider _search;
    private readonly IVectorSearchProvider _vectors;

    public SearchWebsiteHandler(IAppDbContext db, ITenantContext tenant, ISearchProvider search, IVectorSearchProvider vectors)
    {
        _db = db;
        _tenant = tenant;
        _search = search;
        _vectors = vectors;
    }

    public async Task<SiteSearchResponse> Handle(Guid businessId, string? query, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            throw AppException.Validation("Enter at least two characters to search the indexed website.");
        }

        var lexical = await _search.SearchAsync(tenantId, businessId, query.Trim(), cancellationToken);
        var vector = _vectors.IsConfigured
            ? await _vectors.SearchAsync(tenantId, businessId, query.Trim(), cancellationToken)
            : [];
        var hits = lexical.Concat(vector)
            .GroupBy(h => h.Url, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(h => h.Score).First())
            .OrderByDescending(h => h.Score)
            .Take(20)
            .ToList();
        var provider = _vectors.IsConfigured ? $"{_search.ProviderCode}+{_vectors.ProviderCode}" : _search.ProviderCode;
        return new SiteSearchResponse(
            provider,
            query.Trim(),
            hits.Select(h => new SiteSearchHitResponse(h.Title, h.Url, h.Snippet, h.Score)).ToList());
    }
}

public sealed class ListTestReportsHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public ListTestReportsHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<TestReportResponse>> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var reports = await _db.TestReports.AsNoTracking()
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(40)
            .ToListAsync(cancellationToken);
        return reports.Select(r => r.ToResponse()).ToList();
    }
}

public sealed class DownloadTestReportPdfHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;
    private readonly ITestReportPdf _pdf;

    public DownloadTestReportPdfHandler(IAppDbContext db, ITenantContext tenant, ICurrentUser user, ITestReportPdf pdf)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
        _pdf = pdf;
    }

    public async Task<(string FileName, byte[] Bytes)> Handle(Guid businessId, Guid reportId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var report = await _db.TestReports.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportId && r.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Report was not found.");
        var preparedFor = await _db.Users.AsNoTracking()
            .Where(u => u.Id == _user.UserId)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(preparedFor))
        {
            preparedFor = string.IsNullOrWhiteSpace(_user.Email) ? "Signed-in operator" : _user.Email;
        }

        var header = new TestReportPdfHeader(preparedFor, DateTimeOffset.UtcNow);
        return ($"digitalpulse-{report.Kind.ToString().ToLowerInvariant()}-{report.CreatedAtUtc:yyyyMMdd}.pdf", _pdf.Render(report, header));
    }
}

public sealed class SearchConsoleWriteHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IOfficialPlatformGateway _gateway;
    private readonly ILiveTokenRefresher _tokens;

    public SearchConsoleWriteHandler(IAppDbContext db, ITenantContext tenant, IOfficialPlatformGateway gateway, ILiveTokenRefresher tokens)
    {
        _db = db;
        _tenant = tenant;
        _gateway = gateway;
        _tokens = tokens;
    }

    public async Task<(bool Held, string Detail)> SubmitSitemapAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var (connection, site) = await RequireLiveAsync(businessId, cancellationToken);
        if (connection is null)
        {
            return (true, site);
        }

        var feed = site + "sitemap.xml";
        var url = $"https://www.googleapis.com/webmasters/v3/sites/{Uri.EscapeDataString(site)}/sitemaps/{Uri.EscapeDataString(feed)}";
        var result = await _gateway.SendAsync(HttpMethod.Put, url, connection.AccessToken, "{}", null, cancellationToken);
        if (result.Ok)
        {
            return (false, "Official Search Console accepted the sitemap submit.");
        }

        return (true, result.StatusCode is 401 or 403
            ? "Search Console rejected the grant. Reauthorize with the webmasters scope."
            : $"Official sitemap submit returned {result.StatusCode}. A write was not invented.");
    }

    public async Task<(bool Held, string Detail)> InspectUrlAsync(Guid businessId, string? targetUrl, CancellationToken cancellationToken)
    {
        var (connection, site) = await RequireLiveAsync(businessId, cancellationToken);
        if (connection is null)
        {
            return (true, site);
        }

        var inspection = string.IsNullOrWhiteSpace(targetUrl) ? site : targetUrl.Trim();
        var body = JsonSerializer.Serialize(new { inspectionUrl = inspection, siteUrl = site });
        var result = await _gateway.SendAsync(
            HttpMethod.Post,
            "https://searchconsole.googleapis.com/v1/urlInspection/index:inspect",
            connection.AccessToken,
            body,
            null,
            cancellationToken);
        if (result.Ok)
        {
            var snippet = result.Body.Length <= 400 ? result.Body : result.Body[..400] + "…";
            return (false, snippet);
        }

        return (true, result.StatusCode is 401 or 403
            ? "Search Console rejected the grant. Reauthorize with the webmasters scope."
            : $"Official URL Inspection returned {result.StatusCode}. A write was not invented.");
    }

    private async Task<(PlatformConnection? Connection, string Detail)> RequireLiveAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var business = await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var connection = await _db.Connections
            .FirstOrDefaultAsync(c => c.BusinessId == businessId && c.PlatformCode == "SEARCH_CONSOLE", cancellationToken);
        if (connection is null)
        {
            return (null, "Search Console write waits for a live OAuth grant. DigitalPulse will not invent a sitemap submit.");
        }

        await _tokens.EnsureFreshAsync(connection, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        if (!connection.HasLiveCredential)
        {
            return (null, "Search Console write waits for a live OAuth grant. DigitalPulse will not invent a sitemap submit.");
        }

        var site = WebsiteMap.NormalizeSite(business.Website);
        return string.IsNullOrWhiteSpace(site)
            ? (null, "Add the official website before a Search Console write.")
            : (connection, site);
    }
}
