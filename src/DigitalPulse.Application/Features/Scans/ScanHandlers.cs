using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Connections;
using DigitalPulse.Application.Features.Identity;
using DigitalPulse.Contracts.Scans;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Scans;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Scans;

internal static class ScanMap
{
    public static EvidenceResponse ToResponse(this FindingEvidence evidence) =>
        new(evidence.Id, evidence.Kind.ToString(), evidence.Label, evidence.Value, evidence.Source);

    public static FindingResponse ToResponse(this Finding finding, IReadOnlyList<FindingEvidence> evidence) =>
        new(
            finding.Id,
            finding.ScanId,
            finding.BusinessId,
            finding.Category,
            finding.Severity.ToString(),
            finding.Title,
            finding.Description,
            finding.ExpectedValue,
            finding.ObservedValue,
            finding.Recommendation,
            finding.SuggestedAction,
            finding.VerificationMethod,
            finding.AutomationState.ToString(),
            finding.Status.ToString(),
            evidence.OrderBy(e => e.CreatedAtUtc).Select(e => e.ToResponse()).ToList());

    public static ScanSummaryResponse ToSummary(this Scan scan, IReadOnlyList<Finding> findings) =>
        new(
            scan.Id,
            scan.BusinessId,
            scan.Trigger.ToString(),
            scan.Status.ToString(),
            scan.StartedAtUtc,
            scan.CompletedAtUtc,
            scan.Summary,
            scan.Error,
            findings.Count,
            findings.Count(f => f.Status == FindingStatus.Open),
            findings.Count(f => f.Severity == FindingSeverity.Critical));

    public static ScanDetailResponse ToDetail(
        this Scan scan,
        IReadOnlyList<Finding> findings,
        IReadOnlyList<FindingEvidence> evidence,
        int scansPerMonth,
        int scansUsedThisMonth) =>
        new(
            scan.Id,
            scan.BusinessId,
            scan.Trigger.ToString(),
            scan.Status.ToString(),
            scan.StartedAtUtc,
            scan.CompletedAtUtc,
            scan.Summary,
            scan.Error,
            scansPerMonth,
            scansUsedThisMonth,
            findings
                .OrderByDescending(f => f.Severity)
                .ThenBy(f => f.Title)
                .Select(f => f.ToResponse(evidence.Where(e => e.FindingId == f.Id).ToList()))
                .ToList());
}

public sealed class GetScanCenterHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetScanCenterHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ScanCenterResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var plan = await GetConnectionCenterHandler.CurrentPlan(_db, tenantId, cancellationToken);
        var used = await ScanQuota.UsedThisMonthAsync(_db, tenantId, cancellationToken);
        var scans = await _db.Scans.AsNoTracking()
            .Where(s => s.BusinessId == businessId)
            .OrderByDescending(s => s.StartedAtUtc)
            .ToListAsync(cancellationToken);
        var scanIds = scans.Select(s => s.Id).ToList();
        var findings = await _db.Findings.AsNoTracking()
            .Where(f => scanIds.Contains(f.ScanId))
            .ToListAsync(cancellationToken);
        var latest = scans.FirstOrDefault();
        ScanDetailResponse? detail = null;
        if (latest is not null)
        {
            var latestFindings = findings.Where(f => f.ScanId == latest.Id).ToList();
            var evidence = await _db.FindingEvidence.AsNoTracking()
                .Where(e => latestFindings.Select(f => f.Id).Contains(e.FindingId))
                .ToListAsync(cancellationToken);
            detail = latest.ToDetail(latestFindings, evidence, plan.ScansPerMonth, used);
        }

        return new ScanCenterResponse(
            scans.Select(s => s.ToSummary(findings.Where(f => f.ScanId == s.Id).ToList())).ToList(),
            detail,
            plan.ScansPerMonth,
            used);
    }
}

public sealed class GetScanHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetScanHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ScanDetailResponse> Handle(Guid businessId, Guid scanId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var plan = await GetConnectionCenterHandler.CurrentPlan(_db, tenantId, cancellationToken);
        var used = await ScanQuota.UsedThisMonthAsync(_db, tenantId, cancellationToken);
        var scan = await _db.Scans.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scanId && s.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Scan was not found.");
        var findings = await _db.Findings.AsNoTracking()
            .Where(f => f.ScanId == scan.Id)
            .ToListAsync(cancellationToken);
        var evidence = await _db.FindingEvidence.AsNoTracking()
            .Where(e => findings.Select(f => f.Id).Contains(e.FindingId))
            .ToListAsync(cancellationToken);
        return scan.ToDetail(findings, evidence, plan.ScansPerMonth, used);
    }
}

public sealed class RunScanHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IWebsiteProbe _probe;
    private readonly IPlatformAdapterCatalog _catalog;

    public RunScanHandler(
        IAppDbContext db,
        ITenantContext tenant,
        IWebsiteProbe probe,
        IPlatformAdapterCatalog catalog)
    {
        _db = db;
        _tenant = tenant;
        _probe = probe;
        _catalog = catalog;
    }

    public async Task<ScanDetailResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var business = await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var plan = await GetConnectionCenterHandler.CurrentPlan(_db, tenantId, cancellationToken);
        var used = await ScanQuota.UsedThisMonthAsync(_db, tenantId, cancellationToken);
        try
        {
            EntitlementRules.EnsureCanRunScan(plan, used);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        var scan = Scan.Start(tenantId, businessId);
        _db.Scans.Add(scan);

        try
        {
            var locations = await _db.Locations.AsNoTracking()
                .Where(l => l.BusinessId == businessId)
                .ToListAsync(cancellationToken);
            var contacts = await _db.ContactPoints.AsNoTracking()
                .Where(c => c.BusinessId == businessId)
                .ToListAsync(cancellationToken);
            var categoryCount = await _db.Categories.CountAsync(c => c.BusinessId == businessId, cancellationToken);
            var serviceCount = await _db.Services.CountAsync(s => s.BusinessId == businessId, cancellationToken);
            var facts = await _db.Facts.AsNoTracking()
                .Where(f => f.BusinessId == businessId)
                .ToListAsync(cancellationToken);
            var connections = await _db.Connections.AsNoTracking()
                .Where(c => c.BusinessId == businessId)
                .ToListAsync(cancellationToken);
            var phones = contacts.Where(c => c.Kind == Domain.Businesses.ContactPointKind.Phone).Select(c => c.Value).ToList();
            var probe = await _probe.ProbeAsync(business.Website, business.Name, phones, cancellationToken);

            var drafts = CheckComparisons.CompareIdentity(business, locations, contacts, categoryCount, serviceCount, facts)
                .Concat(CheckComparisons.CompareWebsite(business, probe, contacts.Where(c => c.Kind == Domain.Businesses.ContactPointKind.Phone).ToList()))
                .Concat(CheckComparisons.CompareConnections(connections, _catalog))
                .ToList();

            var findings = new List<Finding>();
            var evidence = new List<FindingEvidence>();
            foreach (var draft in drafts)
            {
                var finding = Finding.Open(
                    tenantId,
                    scan.Id,
                    businessId,
                    draft.Category,
                    draft.Severity,
                    draft.Title,
                    draft.Description,
                    draft.ExpectedValue,
                    draft.ObservedValue,
                    draft.Recommendation,
                    draft.SuggestedAction,
                    draft.VerificationMethod,
                    draft.AutomationState);
                _db.Findings.Add(finding);
                findings.Add(finding);
                foreach (var item in draft.Evidence)
                {
                    var row = FindingEvidence.Create(tenantId, finding.Id, item.Kind, item.Label, item.Value, item.Source);
                    _db.FindingEvidence.Add(row);
                    evidence.Add(row);
                }
            }

            scan.Complete(ScanQuota.Summarize(findings));
            await _db.SaveChangesAsync(cancellationToken);
            return scan.ToDetail(findings, evidence, plan.ScansPerMonth, used + 1);
        }
        catch (Exception ex) when (ex is not AppException)
        {
            scan.Fail(ex.Message);
            await _db.SaveChangesAsync(cancellationToken);
            throw AppException.Validation("DigitalPulse Check failed before findings could be stored.");
        }
    }
}

public sealed class UpdateFindingHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public UpdateFindingHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<FindingResponse> Handle(
        Guid businessId,
        Guid findingId,
        UpdateFindingRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        if (!Enum.TryParse<FindingStatus>(request.Status, true, out var status))
        {
            throw AppException.Validation("Finding status must be Open, Acknowledged, or Resolved.");
        }

        var finding = await _db.Findings
            .FirstOrDefaultAsync(f => f.Id == findingId && f.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Finding was not found.");
        finding.SetStatus(status);
        await _db.SaveChangesAsync(cancellationToken);
        var evidence = await _db.FindingEvidence.AsNoTracking()
            .Where(e => e.FindingId == finding.Id)
            .ToListAsync(cancellationToken);
        return finding.ToResponse(evidence);
    }
}

internal static class ScanQuota
{
    public static DateTimeOffset MonthStartUtc(DateTimeOffset now)
    {
        var utc = now.ToUniversalTime();
        return new DateTimeOffset(utc.Year, utc.Month, 1, 0, 0, 0, TimeSpan.Zero);
    }

    public static async Task<int> UsedThisMonthAsync(IAppDbContext db, Guid tenantId, CancellationToken cancellationToken)
    {
        var start = MonthStartUtc(DateTimeOffset.UtcNow);
        return await db.Scans.CountAsync(s => s.TenantId == tenantId && s.StartedAtUtc >= start, cancellationToken);
    }

    public static string Summarize(IReadOnlyCollection<Finding> findings)
    {
        var critical = findings.Count(f => f.Severity == FindingSeverity.Critical);
        var high = findings.Count(f => f.Severity == FindingSeverity.High);
        return $"{findings.Count} finding(s) · {critical} critical · {high} high";
    }
}
