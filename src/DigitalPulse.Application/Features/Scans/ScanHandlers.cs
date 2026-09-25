using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Connections;
using DigitalPulse.Application.Features.Identity;
using DigitalPulse.Application.Features.Website;
using DigitalPulse.Contracts.Scans;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Scans;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Scans;

internal static class ScanMap
{
    public static EvidenceResponse ToResponse(this FindingEvidence evidence) =>
        new(evidence.Id, evidence.Kind.ToString(), evidence.Label, evidence.Value, evidence.Source);

    public static FindingResponse ToResponse(
        this Finding finding,
        IReadOnlyList<FindingEvidence> evidence,
        IReadOnlyList<FindingStep>? steps = null) =>
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
            finding.ResolutionPath,
            finding.PlaybookCode,
            finding.VerifiedAtUtc,
            (steps ?? []).OrderBy(s => s.Ordinal).Select(s =>
                new FindingStepResponse(s.Id, s.Ordinal, s.Title, s.Detail, s.OfficialUrl, s.CompletedAtUtc)).ToList(),
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
        int scansUsedThisMonth,
        IReadOnlyList<FindingStep>? steps = null) =>
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
                .Select(f => f.ToResponse(
                    evidence.Where(e => e.FindingId == f.Id).ToList(),
                    (steps ?? []).Where(s => s.FindingId == f.Id).ToList()))
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
            var steps = await _db.FindingSteps.AsNoTracking()
                .Where(s => latestFindings.Select(f => f.Id).Contains(s.FindingId))
                .ToListAsync(cancellationToken);
            detail = latest.ToDetail(latestFindings, evidence, plan.ScansPerMonth, used, steps);
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
        var steps = await _db.FindingSteps.AsNoTracking()
            .Where(s => findings.Select(f => f.Id).Contains(s.FindingId))
            .ToListAsync(cancellationToken);
        return scan.ToDetail(findings, evidence, plan.ScansPerMonth, used, steps);
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
            var latestSnapshot = await _db.WebsiteSnapshots.AsNoTracking()
                .Where(s => s.BusinessId == businessId)
                .OrderByDescending(s => s.FetchedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            var runId = latestSnapshot?.AuditRunId;
            var snapshotIds = runId is null
                ? []
                : await _db.WebsiteSnapshots.AsNoTracking()
                    .Where(s => s.BusinessId == businessId && s.AuditRunId == runId)
                    .Select(s => s.Id)
                    .ToListAsync(cancellationToken);
            var siteObservations = snapshotIds.Count == 0
                ? []
                : await _db.SearchObservations.AsNoTracking()
                    .Where(o => snapshotIds.Contains(o.SnapshotId))
                    .ToListAsync(cancellationToken);
            var gscQueries = runId is null
                ? []
                : await _db.SearchConsoleQueries.AsNoTracking()
                    .Where(q => q.BusinessId == businessId && q.AuditRunId == runId)
                    .ToListAsync(cancellationToken);
            var adsReport = await _db.TestReports.AsNoTracking()
                .Where(r => r.BusinessId == businessId && r.Kind == DigitalPulse.Domain.Website.TestReportKind.GoogleAds)
                .OrderByDescending(r => r.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            var gaReport = await _db.TestReports.AsNoTracking()
                .Where(r => r.BusinessId == businessId && r.Kind == DigitalPulse.Domain.Website.TestReportKind.GoogleAnalytics)
                .OrderByDescending(r => r.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            var drafts = CheckComparisons.CompareIdentity(business, locations, contacts, categoryCount, serviceCount, facts)
                .Concat(CheckComparisons.CompareWebsite(business, probe, contacts.Where(c => c.Kind == Domain.Businesses.ContactPointKind.Phone).ToList()))
                .Concat(CheckComparisons.CompareConnections(connections, _catalog))
                .Concat(CheckComparisons.CompareSiteAudit(siteObservations))
                .Concat(CheckComparisons.CompareSearch(gscQueries))
                .Concat(CheckComparisons.CompareGoogleMetrics("Ads", "Google Ads", adsReport))
                .Concat(CheckComparisons.CompareGoogleMetrics("Analytics", "Google Analytics", gaReport))
                .ToList();

            var findings = new List<Finding>();
            var evidence = new List<FindingEvidence>();
            var steps = new List<FindingStep>();
            foreach (var draft in drafts)
            {
                var playbook = FindingPlaybookCatalog.Assign(draft.Category, draft.AutomationState, draft.Title);
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
                    draft.AutomationState,
                    playbook.Path,
                    playbook.Code);
                _db.Findings.Add(finding);
                findings.Add(finding);
                var ordinal = 1;
                foreach (var step in FindingPlaybookCatalog.Steps(playbook.Code, draft.ExpectedValue, draft.ObservedValue))
                {
                    var row = FindingStep.Create(tenantId, finding.Id, ordinal++, step.Title, step.Detail, step.OfficialUrl);
                    _db.FindingSteps.Add(row);
                    steps.Add(row);
                }
                foreach (var item in draft.Evidence)
                {
                    var row = FindingEvidence.Create(tenantId, finding.Id, item.Kind, item.Label, item.Value, item.Source);
                    _db.FindingEvidence.Add(row);
                    evidence.Add(row);
                }
            }

            _db.TestReports.Add(DigitalPulse.Domain.Website.TestReport.Assemble(
                tenantId,
                businessId,
                DigitalPulse.Domain.Website.TestReportKind.DigitalPulseCheck,
                "DigitalPulse Check",
                findings.Count == 0
                    ? "No evidence-backed gaps on this check."
                    : $"{findings.Count} evidence-backed signal(s). Product descriptions were not scored.",
                "Follow the playbook or official write, then verify before resolve.",
                string.Empty,
                string.Join('\n', findings.Select(f => $"{f.Severity}: {f.Title}")),
                runId));

            scan.Complete(ScanQuota.Summarize(findings));
            await DigitalPulse.Application.Features.Billing.UsageMeter.RecordAsync(_db, tenantId, DigitalPulse.Domain.Billing.UsageKind.Scan, "digitalpulse-check", cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return scan.ToDetail(findings, evidence, plan.ScansPerMonth, used + 1, steps);
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
        try
        {
            finding.SetStatus(status);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        var evidence = await _db.FindingEvidence.AsNoTracking()
            .Where(e => e.FindingId == finding.Id)
            .ToListAsync(cancellationToken);
        var steps = await _db.FindingSteps.AsNoTracking()
            .Where(s => s.FindingId == finding.Id)
            .ToListAsync(cancellationToken);
        return finding.ToResponse(evidence, steps);
    }
}

public sealed class CompleteFindingStepHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public CompleteFindingStepHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<FindingResponse> Handle(Guid businessId, Guid findingId, Guid stepId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var finding = await _db.Findings.FirstOrDefaultAsync(f => f.Id == findingId && f.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Finding was not found.");
        var step = await _db.FindingSteps.FirstOrDefaultAsync(s => s.Id == stepId && s.FindingId == finding.Id, cancellationToken)
            ?? throw AppException.NotFound("Playbook step was not found.");
        step.Complete();
        await _db.SaveChangesAsync(cancellationToken);
        var evidence = await _db.FindingEvidence.AsNoTracking()
            .Where(e => e.FindingId == finding.Id)
            .ToListAsync(cancellationToken);
        var steps = await _db.FindingSteps.AsNoTracking()
            .Where(s => s.FindingId == finding.Id)
            .ToListAsync(cancellationToken);
        return finding.ToResponse(evidence, steps);
    }
}

public sealed class VerifyFindingHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly AnalyzeWebsiteHandler _website;

    public VerifyFindingHandler(IAppDbContext db, ITenantContext tenant, AnalyzeWebsiteHandler website)
    {
        _db = db;
        _tenant = tenant;
        _website = website;
    }

    public async Task<FindingResponse> Handle(Guid businessId, Guid findingId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var finding = await _db.Findings.FirstOrDefaultAsync(f => f.Id == findingId && f.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Finding was not found.");
        var steps = await _db.FindingSteps.Where(s => s.FindingId == finding.Id).ToListAsync(cancellationToken);
        if (finding.ResolutionPath == "AssistedPlaybook" && steps.Count > 0 && steps.Any(s => s.CompletedAtUtc is null))
        {
            throw AppException.Validation("Complete every playbook step before verifying this signal.");
        }

        if (finding.ResolutionPath is "AssistedPlaybook" or "OfficialWrite"
            && (finding.Category is "Website" or "Page" or "Vision" or "Contact" or "Seo" or "Visibility" or "SearchConsole"))
        {
            await _website.Handle(businessId, cancellationToken);
        }

        var latest = await _db.WebsiteSnapshots.AsNoTracking()
            .Where(s => s.BusinessId == businessId)
            .OrderByDescending(s => s.FetchedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var stillOpen = false;
        if (latest?.AuditRunId is Guid runId)
        {
            var snapshotIds = await _db.WebsiteSnapshots.AsNoTracking()
                .Where(s => s.AuditRunId == runId)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);
            stillOpen = await _db.SearchObservations.AsNoTracking()
                .AnyAsync(o => snapshotIds.Contains(o.SnapshotId) && o.Title == finding.Title, cancellationToken);
        }

        if (finding.ResolutionPath == "ConnectFirst")
        {
            var connected = await _db.Connections.AsNoTracking()
                .AnyAsync(c => c.BusinessId == businessId && c.Status == Domain.Platforms.ConnectionStatus.Connected && c.HasLiveCredential, cancellationToken);
            if (!connected)
            {
                throw AppException.Validation("A live platform grant is still missing. Connect the official account first.");
            }

            stillOpen = false;
        }

        if (stillOpen)
        {
            throw AppException.Validation("The latest crawl still observes this gap. The signal stays open.");
        }

        finding.MarkVerified();
        finding.SetStatus(FindingStatus.Resolved);
        await _db.SaveChangesAsync(cancellationToken);
        var evidence = await _db.FindingEvidence.AsNoTracking()
            .Where(e => e.FindingId == finding.Id)
            .ToListAsync(cancellationToken);
        steps = await _db.FindingSteps.AsNoTracking()
            .Where(s => s.FindingId == finding.Id)
            .ToListAsync(cancellationToken);
        return finding.ToResponse(evidence, steps);
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
