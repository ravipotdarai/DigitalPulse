using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Contracts.Onboarding;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Scans;
using DigitalPulse.Domain.Social;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Dashboard;

public sealed class GetDashboardHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ISearchProvider _search;

    public GetDashboardHandler(IAppDbContext db, ITenantContext tenantContext, ISearchProvider search)
    {
        _db = db;
        _tenantContext = tenantContext;
        _search = search;
    }

    public async Task<DashboardResponse> Handle(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var tenant = await _db.Tenants.AsNoTracking().FirstAsync(t => t.Id == tenantId, cancellationToken);
        var subscription = await _db.Subscriptions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken)
            ?? throw AppException.Validation("Complete plan selection before opening the dashboard.");

        var plan = await _db.Plans.AsNoTracking().FirstAsync(p => p.Id == subscription.PlanId, cancellationToken);
        var businesses = await _db.Businesses.AsNoTracking()
            .Where(b => b.TenantId == tenantId)
            .Select(b => new DashboardBusinessResponse(
                b.Id,
                b.Name,
                b.Website,
                _db.Locations.Count(l => l.BusinessId == b.Id && l.TenantId == tenantId)))
            .ToListAsync(cancellationToken);

        var latestScan = await _db.Scans.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.Status == ScanStatus.Completed)
            .OrderByDescending(s => s.CompletedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var latestFindings = latestScan is null
            ? []
            : await _db.Findings.AsNoTracking()
                .Where(f => f.ScanId == latestScan.Id)
                .ToListAsync(cancellationToken);
        var top = latestFindings
            .Where(f => f.Status == FindingStatus.Open)
            .OrderByDescending(f => f.Severity)
            .ThenBy(f => f.Title)
            .Take(5)
            .Select(f => new DashboardFindingResponse(f.Id, f.BusinessId, f.Severity.ToString(), f.Category, f.Title, f.Status.ToString()))
            .ToList();

        var latestWebsite = await _db.WebsiteSnapshots.AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.FetchedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var websiteObservations = latestWebsite is null
            ? 0
            : await _db.SearchObservations.CountAsync(o => o.SnapshotId == latestWebsite.Id, cancellationToken);

        return new DashboardResponse(
            tenant.Id,
            tenant.Name,
            tenant.Type.ToString(),
            plan.Name,
            plan.MaxBusinesses,
            businesses.Count,
            businesses.Sum(b => b.LocationCount),
            businesses,
            latestScan?.CompletedAtUtc,
            latestFindings.Count(f => f.Status == FindingStatus.Open),
            latestFindings.Count(f => f.Status == FindingStatus.Open && f.Severity >= FindingSeverity.High),
            top,
            latestWebsite?.FetchedAtUtc,
            websiteObservations,
            _search.ProviderCode,
            await _db.SocialContent.CountAsync(c => c.TenantId == tenantId && c.Status == SocialContentStatus.Draft, cancellationToken),
            await _db.SocialContent.CountAsync(c => c.TenantId == tenantId && c.Status == SocialContentStatus.Blocked, cancellationToken));
    }
}
