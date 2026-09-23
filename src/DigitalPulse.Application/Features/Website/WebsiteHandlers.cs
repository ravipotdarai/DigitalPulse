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

    public static SearchConsoleStatusResponse ConsoleStatus(PlatformConnection? connection)
    {
        if (connection is null || connection.Status != ConnectionStatus.Connected)
        {
            return new("NotConnected", connection?.GrantKind, "Search Console is not connected. Impressions and queries are not invented.");
        }

        if (string.Equals(connection.GrantKind, "Development", StringComparison.OrdinalIgnoreCase))
        {
            return new("Hold", connection.GrantKind, "Development grant only. Search Console coverage is not invented.");
        }

        return new(connection.Status.ToString(), connection.GrantKind, "Live Search Console reads are not implemented in this phase.");
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
        var snapshot = await _db.WebsiteSnapshots.AsNoTracking()
            .Where(s => s.BusinessId == businessId)
            .OrderByDescending(s => s.FetchedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var observations = snapshot is null
            ? []
            : await _db.SearchObservations.AsNoTracking()
                .Where(o => o.SnapshotId == snapshot.Id)
                .OrderByDescending(o => o.Severity)
                .ThenBy(o => o.Title)
                .ToListAsync(cancellationToken);
        var gsc = await _db.Connections.AsNoTracking()
            .FirstOrDefaultAsync(c => c.BusinessId == businessId && c.PlatformCode == "SEARCH_CONSOLE", cancellationToken);

        return new WebsiteIntelligenceResponse(
            snapshot?.ToResponse(),
            observations.Select(o => o.ToResponse()).ToList(),
            WebsiteMap.ConsoleStatus(gsc),
            _search.ProviderCode,
            _vectors.IsConfigured);
    }
}

public sealed class AnalyzeWebsiteHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IWebsiteFetcher _fetcher;
    private readonly ISearchProvider _search;
    private readonly IVectorSearchProvider _vectors;

    public AnalyzeWebsiteHandler(
        IAppDbContext db,
        ITenantContext tenant,
        IWebsiteFetcher fetcher,
        ISearchProvider search,
        IVectorSearchProvider vectors)
    {
        _db = db;
        _tenant = tenant;
        _fetcher = fetcher;
        _search = search;
        _vectors = vectors;
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
            .Select(c => c.Value)
            .ToListAsync(cancellationToken);
        var connections = await _db.Connections.AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .ToListAsync(cancellationToken);
        var fetch = await _fetcher.FetchAsync(business.Website, cancellationToken);
        var signals = fetch.Reached ? HtmlSignalParser.Parse(fetch.Html) : null;
        var phone = phones.FirstOrDefault();
        var snapshot = WebsiteSnapshot.Record(
            tenantId,
            businessId,
            fetch.FinalUrl ?? business.Website,
            StatusOf(fetch),
            fetch.StatusCode,
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
            fetch.Error);
        _db.WebsiteSnapshots.Add(snapshot);

        var drafts = WebsiteObservations.FromSnapshot(business.Name, snapshot, signals, connections);
        var observations = drafts.Select(d => SearchObservation.Create(
            tenantId,
            businessId,
            snapshot.Id,
            d.Category,
            d.Severity,
            d.Title,
            d.Detail,
            d.ExpectedValue,
            d.ObservedValue,
            d.Recommendation)).ToList();
        foreach (var observation in observations)
        {
            _db.SearchObservations.Add(observation);
        }

        if (fetch.Reached && signals is not null)
        {
            await _search.IndexAsync(
                new SearchDocument(
                    tenantId,
                    businessId,
                    "Website",
                    signals.Title ?? business.Name,
                    signals.Text,
                    snapshot.Url ?? business.Website),
                cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        var gsc = connections.FirstOrDefault(c => c.PlatformCode.Equals("SEARCH_CONSOLE", StringComparison.OrdinalIgnoreCase));
        return new WebsiteIntelligenceResponse(
            snapshot.ToResponse(),
            observations.OrderByDescending(o => o.Severity).ThenBy(o => o.Title).Select(o => o.ToResponse()).ToList(),
            WebsiteMap.ConsoleStatus(gsc),
            _search.ProviderCode,
            _vectors.IsConfigured);
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

    public SearchWebsiteHandler(IAppDbContext db, ITenantContext tenant, ISearchProvider search)
    {
        _db = db;
        _tenant = tenant;
        _search = search;
    }

    public async Task<SiteSearchResponse> Handle(Guid businessId, string? query, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            throw AppException.Validation("Enter at least two characters to search the indexed website.");
        }

        var hits = await _search.SearchAsync(tenantId, businessId, query.Trim(), cancellationToken);
        return new SiteSearchResponse(
            _search.ProviderCode,
            query.Trim(),
            hits.Select(h => new SiteSearchHitResponse(h.Title, h.Url, h.Snippet, h.Score)).ToList());
    }
}
