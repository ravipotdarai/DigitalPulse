using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Identity;
using DigitalPulse.Contracts.Monitoring;
using DigitalPulse.Domain.Actions;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Monitoring;
using DigitalPulse.Domain.Platforms;
using DigitalPulse.Domain.Social;
using DigitalPulse.Domain.WhatsApp;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Monitoring;

internal static class MonitoringMaps
{
    public static MonitoringKindResponse ToResponse(this MonitoringKindDescriptor kind) =>
        new(kind.Code, kind.Name, kind.CanObserveWithoutLiveApi, kind.Purpose);

    public static MonitoringScheduleResponse ToResponse(this MonitoringSchedule schedule) =>
        new(
            schedule.IntervalHours,
            schedule.Enabled,
            schedule.LastRunAtUtc,
            schedule.NextRunAtUtc,
            MonitoringPolicy.IsDue(schedule.NextRunAtUtc),
            schedule.HoldReason);

    public static MonitoringResultResponse ToResponse(this MonitoringResult result) =>
        new(
            result.Id,
            MonitoringCatalog.All.First(k => k.Kind == result.Kind).Code,
            result.Status.ToString(),
            result.Title,
            result.ObservedFact,
            result.Recommendation,
            result.PreviousValue,
            result.CurrentValue);

    public static MonitoringRunResponse ToResponse(this MonitoringRun run, IReadOnlyList<MonitoringResult> results) =>
        new(
            run.Id,
            run.Trigger.ToString(),
            run.Status.ToString(),
            run.Summary,
            run.StartedAtUtc,
            run.CompletedAtUtc,
            results.OrderBy(r => r.Kind).Select(r => r.ToResponse()).ToList());

    public static MonitoringAlertResponse ToResponse(this MonitoringAlert alert) =>
        new(alert.Id, alert.Severity.ToString(), alert.Status.ToString(), alert.Title, alert.Detail, alert.OpenedAtUtc, alert.AcknowledgedAtUtc);

    public static CompetitorResponse ToResponse(this Competitor competitor) =>
        new(competitor.Id, competitor.Name, competitor.Website, competitor.Notes);

    public static PresenceReportResponse ToResponse(this PresenceReport report) =>
        new(
            report.Id,
            report.Kind.ToString(),
            report.Title,
            report.ObservedFact,
            report.Recommendation,
            report.AiInterpretation,
            report.CustomerDecision,
            report.PeriodStartUtc,
            report.PeriodEndUtc,
            report.HoldReason);

    public static string Clip(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)].TrimEnd() + "…";
}

internal static class MonitoringStore
{
    public static async Task<SubscriptionPlan> PlanAsync(IAppDbContext db, Guid tenantId, CancellationToken cancellationToken)
    {
        var subscription = await db.Subscriptions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken)
            ?? throw AppException.Validation("Complete plan selection before opening monitoring.");
        return await db.Plans.AsNoTracking().FirstAsync(p => p.Id == subscription.PlanId, cancellationToken);
    }

    public static int IntervalHours(SubscriptionPlan plan) =>
        plan.MonitoringIntervalHours > 0
            ? plan.MonitoringIntervalHours
            : MonitoringPolicy.IntervalHoursFor(plan.Code);

    public static async Task<MonitoringSchedule> EnsureScheduleAsync(
        IAppDbContext db,
        Guid tenantId,
        Guid businessId,
        int intervalHours,
        CancellationToken cancellationToken)
    {
        var schedule = await db.MonitoringSchedules.FirstOrDefaultAsync(s => s.BusinessId == businessId, cancellationToken);
        if (schedule is null)
        {
            schedule = MonitoringSchedule.Create(tenantId, businessId, intervalHours);
            db.MonitoringSchedules.Add(schedule);
            return schedule;
        }

        schedule.SyncInterval(intervalHours);
        return schedule;
    }

    public static async Task<MonitoringWorkspaceResponse> LoadAsync(IAppDbContext db, Guid businessId, CancellationToken cancellationToken)
    {
        var schedule = await db.MonitoringSchedules.AsNoTracking().FirstAsync(s => s.BusinessId == businessId, cancellationToken);
        var runs = await db.MonitoringRuns.AsNoTracking()
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.StartedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);
        var results = await db.MonitoringResults.AsNoTracking()
            .Where(r => runs.Select(x => x.Id).Contains(r.RunId))
            .ToListAsync(cancellationToken);
        var alerts = await db.MonitoringAlerts.AsNoTracking()
            .Where(a => a.BusinessId == businessId)
            .OrderByDescending(a => a.OpenedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);
        var competitors = await db.Competitors.AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
        var reports = await db.PresenceReports.AsNoTracking()
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);

        return new MonitoringWorkspaceResponse(
            schedule.ToResponse(),
            "Monitoring records stored health, a safe website probe, and honest holds. Search, social live metrics, reviews, competitor listings, and provider API failures are not invented.",
            MonitoringCatalog.All.Select(k => k.ToResponse()).ToList(),
            runs.Select(run => run.ToResponse(results.Where(r => r.RunId == run.Id).ToList())).ToList(),
            alerts.Select(a => a.ToResponse()).ToList(),
            competitors.Select(c => c.ToResponse()).ToList(),
            reports.Select(r => r.ToResponse()).ToList());
    }
}

public static class MonitoringEngine
{
    public static async Task<MonitoringWorkspaceResponse> RunAsync(
        IAppDbContext db,
        IWebsiteFetcher fetcher,
        IPlatformAdapterCatalog adapters,
        Guid tenantId,
        Guid businessId,
        MonitoringTrigger trigger,
        CancellationToken cancellationToken)
    {
        var business = await db.Businesses.FirstOrDefaultAsync(b => b.Id == businessId && b.TenantId == tenantId, cancellationToken)
            ?? throw AppException.NotFound("Business was not found.");
        var plan = await MonitoringStore.PlanAsync(db, tenantId, cancellationToken);
        var interval = MonitoringStore.IntervalHours(plan);
        var schedule = await MonitoringStore.EnsureScheduleAsync(db, tenantId, businessId, interval, cancellationToken);
        if (trigger == MonitoringTrigger.Scheduled && !MonitoringPolicy.IsDue(schedule.NextRunAtUtc))
        {
            await db.SaveChangesAsync(cancellationToken);
            return await MonitoringStore.LoadAsync(db, businessId, cancellationToken);
        }

        var run = MonitoringRun.Start(tenantId, businessId, trigger);
        db.MonitoringRuns.Add(run);

        var previous = await db.MonitoringResults.AsNoTracking()
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var previousByKind = previous
            .GroupBy(r => r.Kind)
            .ToDictionary(g => g.Key, g => g.First());

        var results = new List<MonitoringResult>();
        foreach (var kind in MonitoringCatalog.All)
        {
            var draft = await ObserveAsync(db, fetcher, adapters, business, kind, cancellationToken);
            previousByKind.TryGetValue(kind.Kind, out var last);
            var status = Classify(draft.Status, last?.CurrentValue, draft.CurrentValue);
            var result = MonitoringResult.Record(
                tenantId,
                run.Id,
                businessId,
                kind.Kind,
                status,
                draft.Title,
                MonitoringMaps.Clip(draft.ObservedFact, 500),
                MonitoringMaps.Clip(draft.Recommendation, 500),
                last?.CurrentValue,
                draft.CurrentValue);
            db.MonitoringResults.Add(result);
            results.Add(result);
            await RaiseAlertAsync(db, tenantId, businessId, result, cancellationToken);
        }

        var competitors = await db.Competitors.Where(c => c.BusinessId == businessId).ToListAsync(cancellationToken);
        foreach (var competitor in competitors)
        {
            db.CompetitorObservations.Add(CompetitorObservation.Hold(
                tenantId,
                competitor.Id,
                run.Id,
                "Competitor is operator-recorded. Official listing, ranking, and review reads stay held."));
        }

        var held = results.Count(r => r.Status == ObservationStatus.Held);
        var changed = results.Count(r => r.Status == ObservationStatus.Changed);
        run.Complete($"{results.Count} checks. {changed} changed. {held} held. Live provider metrics were not invented.");
        schedule.MarkRan(DateTimeOffset.UtcNow, run.Summary);
        await db.SaveChangesAsync(cancellationToken);
        return await MonitoringStore.LoadAsync(db, businessId, cancellationToken);
    }

    public static async Task<MonitoringWorkspaceResponse> AssembleReportAsync(
        IAppDbContext db,
        Guid tenantId,
        Guid businessId,
        CancellationToken cancellationToken)
    {
        await MonitoringStore.PlanAsync(db, tenantId, cancellationToken);
        var latest = await db.MonitoringRuns.AsNoTracking()
            .Where(r => r.BusinessId == businessId && r.Status == MonitoringRunStatus.Completed)
            .OrderByDescending(r => r.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var results = latest is null
            ? []
            : await db.MonitoringResults.AsNoTracking().Where(r => r.RunId == latest.Id).OrderBy(r => r.Kind).ToListAsync(cancellationToken);

        var observed = results.Count == 0
            ? "No monitoring run is stored yet. The report does not invent platform metrics."
            : string.Join(" ", results.Select(r => $"{r.Title}: {r.ObservedFact}"));
        var recommendation = results.Count == 0
            ? "Run monitoring, then assemble the report from stored observations."
            : string.Join(" ", results.Select(r => r.Recommendation).Distinct());

        var report = PresenceReport.Assemble(
            tenantId,
            businessId,
            ReportKind.Pulse,
            "Presence pulse",
            MonitoringMaps.Clip(observed, 4000),
            MonitoringMaps.Clip(recommendation, 4000),
            "No AI interpretation. This report is assembled from stored observations only.",
            latest?.StartedAtUtc ?? DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow,
            "Live provider metrics and model-written commentary stay held.");
        db.PresenceReports.Add(report);
        await db.SaveChangesAsync(cancellationToken);
        return await MonitoringStore.LoadAsync(db, businessId, cancellationToken);
    }

    private static ObservationStatus Classify(ObservationStatus proposed, string? previous, string? current)
    {
        if (proposed == ObservationStatus.Held)
        {
            return ObservationStatus.Held;
        }

        if (string.IsNullOrWhiteSpace(previous))
        {
            return ObservationStatus.Observed;
        }

        return string.Equals(previous, current, StringComparison.Ordinal)
            ? ObservationStatus.Unchanged
            : ObservationStatus.Changed;
    }

    private static async Task RaiseAlertAsync(
        IAppDbContext db,
        Guid tenantId,
        Guid businessId,
        MonitoringResult result,
        CancellationToken cancellationToken)
    {
        AlertSeverity? severity = result.Status switch
        {
            ObservationStatus.Changed when result.Kind is MonitoringKind.WebsiteAvailability or MonitoringKind.ActionFailures => AlertSeverity.High,
            ObservationStatus.Changed => AlertSeverity.Warning,
            ObservationStatus.Observed when result.Kind == MonitoringKind.ActionFailures && result.CurrentValue is not null && result.CurrentValue != "failed:0|escalated:0|assisted:0" => AlertSeverity.Warning,
            ObservationStatus.Observed when result.Kind == MonitoringKind.WebsiteAvailability && result.CurrentValue is "unreachable" or "missing" => AlertSeverity.High,
            _ => null
        };
        if (severity is null)
        {
            return;
        }

        var exists = await db.MonitoringAlerts.AnyAsync(
            a => a.BusinessId == businessId && a.Status == AlertStatus.Open && a.Title == result.Title,
            cancellationToken);
        if (exists)
        {
            return;
        }

        db.MonitoringAlerts.Add(MonitoringAlert.Open(tenantId, businessId, result.Id, severity.Value, result.Title, result.ObservedFact));
    }

    private static async Task<(ObservationStatus Status, string Title, string ObservedFact, string Recommendation, string? CurrentValue)> ObserveAsync(
        IAppDbContext db,
        IWebsiteFetcher fetcher,
        IPlatformAdapterCatalog adapters,
        Business business,
        MonitoringKindDescriptor kind,
        CancellationToken cancellationToken)
    {
        return kind.Kind switch
        {
            MonitoringKind.PlatformHealth => await PlatformAsync(db, adapters, business, cancellationToken),
            MonitoringKind.WebsiteAvailability => await WebsiteAsync(fetcher, business, cancellationToken),
            MonitoringKind.IdentityConsistency => await IdentityAsync(db, business, cancellationToken),
            MonitoringKind.SearchVisibility => Held("Search visibility", "Search Console and ranking reads stay held. DigitalPulse did not invent impressions or positions."),
            MonitoringKind.SocialActivity => await SocialAsync(db, business.Id, cancellationToken),
            MonitoringKind.WhatsAppHealth => await WhatsAppAsync(db, business.Id, cancellationToken),
            MonitoringKind.ReviewChanges => Held("Review changes", "Official review feeds are not connected. Star counts and new reviews were not invented."),
            MonitoringKind.ProfileChanges => await IdentityAsync(db, business, cancellationToken, profile: true),
            MonitoringKind.CompetitorChanges => await CompetitorsAsync(db, business.Id, cancellationToken),
            MonitoringKind.PublishedContent => await PublishedAsync(db, business.Id, cancellationToken),
            MonitoringKind.ActionFailures => await ActionsAsync(db, business.Id, cancellationToken),
            MonitoringKind.ApiFailures => Held("API failures", "Provider API failures are recorded only when a live adapter reports them. None were invented."),
            _ => Held(kind.Name, kind.Purpose)
        };
    }

    private static (ObservationStatus Status, string Title, string ObservedFact, string Recommendation, string? CurrentValue) Held(string title, string fact) =>
        (ObservationStatus.Held, title, fact, "Connect an official live API before treating this as an observed metric.", "held");

    private static async Task<(ObservationStatus, string, string, string, string?)> PlatformAsync(
        IAppDbContext db,
        IPlatformAdapterCatalog adapters,
        Business business,
        CancellationToken cancellationToken)
    {
        var connections = await db.Connections.AsNoTracking().Where(c => c.BusinessId == business.Id).ToListAsync(cancellationToken);
        if (connections.Count == 0)
        {
            return (ObservationStatus.Observed, "Platform connection health", "No authorized connections are stored.", "Connect an official adapter from Connection Center. Development grants do not invent live status.", "connected:0/0");
        }

        var holds = 0;
        foreach (var connection in connections)
        {
            var adapter = adapters.All().FirstOrDefault(a => a.Describe().Code.Equals(connection.PlatformCode, StringComparison.OrdinalIgnoreCase));
            if (adapter is null)
            {
                continue;
            }

            var health = await adapter.HealthCheckAsync(connection, cancellationToken);
            if (health.Status.Equals("Hold", StringComparison.OrdinalIgnoreCase) ||
                health.Status.Equals("Unavailable", StringComparison.OrdinalIgnoreCase))
            {
                holds++;
            }
        }

        var connected = connections.Count(c => c.Status == ConnectionStatus.Connected);
        var errors = connections.Count(c => c.Status is ConnectionStatus.Error or ConnectionStatus.NeedsReauth);
        var value = $"connected:{connected}/{connections.Count}|errors:{errors}|adapter-holds:{holds}";
        return (
            ObservationStatus.Observed,
            "Platform connection health",
            $"{connected} of {connections.Count} stored connections are Connected. {holds} adapter health check(s) stayed on hold. Live provider status was not invented.",
            errors > 0 ? "Re-authorize connections in Error or NeedsReauth." : "Stored grants are enough for DigitalPulse Check. Live health waits for official APIs.",
            value);
    }

    private static async Task<(ObservationStatus, string, string, string, string?)> WebsiteAsync(
        IWebsiteFetcher fetcher,
        Business business,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(business.Website))
        {
            return (ObservationStatus.Observed, "Website availability", "The identity record has no official website.", "Add the official homepage before availability can be observed.", "missing");
        }

        var fetch = await fetcher.FetchAsync(business.Website, cancellationToken);
        if (fetch.Disabled)
        {
            return (ObservationStatus.Held, "Website availability", "Website fetch is disabled in this host.", "Enable the safe homepage probe in this environment.", "disabled");
        }

        if (fetch.Blocked)
        {
            return (ObservationStatus.Observed, "Website availability", fetch.Error ?? "The website URL was blocked by the safe-fetch policy.", "Use a public https homepage. Private and metadata addresses are rejected.", "blocked");
        }

        if (fetch.Reached)
        {
            return (ObservationStatus.Observed, "Website availability", $"Safe homepage probe reached {fetch.FinalUrl} with HTTP {fetch.StatusCode}. This is not a Search Console crawl.", "Keep the official homepage reachable. Search impressions stay held.", $"http:{fetch.StatusCode}");
        }

        return (ObservationStatus.Observed, "Website availability", fetch.Error ?? "The official homepage was not reached.", "Fix DNS or hosting, then run monitoring again.", "unreachable");
    }

    private static async Task<(ObservationStatus, string, string, string, string?)> IdentityAsync(
        IAppDbContext db,
        Business business,
        CancellationToken cancellationToken,
        bool profile = false)
    {
        var locations = await db.Locations.CountAsync(l => l.BusinessId == business.Id, cancellationToken);
        var contacts = await db.ContactPoints.CountAsync(c => c.BusinessId == business.Id, cancellationToken);
        var categories = await db.Categories.CountAsync(c => c.BusinessId == business.Id, cancellationToken);
        var value = $"{business.Name}|{business.Website}|locs:{locations}|contacts:{contacts}|cats:{categories}";
        var title = profile ? "Profile changes" : "Business identity consistency";
        return (
            ObservationStatus.Observed,
            title,
            $"Stored identity fingerprint is {business.Name}, website {(business.Website ?? "none")}, {locations} location(s), {contacts} contact(s), {categories} categor(y/ies).",
            "Change detection compares this fingerprint to the previous monitoring run. Listings are not scraped.",
            value);
    }

    private static async Task<(ObservationStatus, string, string, string, string?)> SocialAsync(
        IAppDbContext db,
        Guid businessId,
        CancellationToken cancellationToken)
    {
        var drafts = await db.SocialContent.CountAsync(c => c.BusinessId == businessId && c.Status == SocialContentStatus.Draft, cancellationToken);
        var blocked = await db.SocialContent.CountAsync(c => c.BusinessId == businessId && c.Status == SocialContentStatus.Blocked, cancellationToken);
        return (
            ObservationStatus.Observed,
            "Social activity",
            $"{drafts} stored draft(s) and {blocked} blocked post(s). Likes, views, and live reach stay held.",
            "Approve drafts in the social workspace. Live metrics wait for an official adapter.",
            $"drafts:{drafts}|blocked:{blocked}|live:held");
    }

    private static async Task<(ObservationStatus, string, string, string, string?)> WhatsAppAsync(
        IAppDbContext db,
        Guid businessId,
        CancellationToken cancellationToken)
    {
        var account = await db.WhatsAppAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.BusinessId == businessId, cancellationToken);
        var optOuts = await db.WhatsAppContacts.CountAsync(c => c.BusinessId == businessId && c.Consent == WhatsAppConsentStatus.OptedOut, cancellationToken);
        var held = await db.WhatsAppMessages.CountAsync(
            m => m.BusinessId == businessId && (m.Status == WhatsAppMessageStatus.Held || m.Status == WhatsAppMessageStatus.Failed),
            cancellationToken);
        var status = account?.Status.ToString() ?? "none";
        return (
            ObservationStatus.Observed,
            "WhatsApp health",
            account is null
                ? "No Cloud API account is stored. Unofficial WhatsApp clients are out of scope."
                : $"Stored account is {account.Status}. Phone {account.PhoneStatus}. {optOuts} opt-out(s). {held} held or failed message(s). Delivery receipts were not invented.",
            account is null
                ? "Connect WhatsApp Cloud API from the WhatsApp workspace."
                : "Review opt-outs and held sends. Live delivery waits for WhatsApp:CloudApi:AccessToken.",
            $"account:{status}|optouts:{optOuts}|held:{held}");
    }

    private static async Task<(ObservationStatus, string, string, string, string?)> CompetitorsAsync(
        IAppDbContext db,
        Guid businessId,
        CancellationToken cancellationToken)
    {
        var names = await db.Competitors.AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .Select(c => c.Name)
            .OrderBy(n => n)
            .ToListAsync(cancellationToken);
        if (names.Count == 0)
        {
            return (ObservationStatus.Observed, "Competitor changes", "No competitors are operator-recorded.", "Add a competitor by name. Official listing reads stay held.", "competitors:0");
        }

        return (
            ObservationStatus.Held,
            "Competitor changes",
            $"{names.Count} competitor(s) recorded ({string.Join(", ", names)}). Official listing, ranking, and review reads stay held.",
            "Keep the operator list current. DigitalPulse will not invent competitor pages.",
            "held");
    }

    private static async Task<(ObservationStatus, string, string, string, string?)> PublishedAsync(
        IAppDbContext db,
        Guid businessId,
        CancellationToken cancellationToken)
    {
        var blocked = await db.SocialContent.CountAsync(c => c.BusinessId == businessId && c.Status == SocialContentStatus.Blocked, cancellationToken);
        var assisted = await db.WorkActions.CountAsync(
            a => a.BusinessId == businessId && a.Kind == ActionKind.PublishSocial && a.Status == ActionStatus.Assisted,
            cancellationToken);
        return (
            ObservationStatus.Observed,
            "Published content",
            $"{blocked} social item(s) blocked from publish and {assisted} assisted publish action(s). Live posts were not invented.",
            "Approve and wait for an official write adapter. Autopilot does not invent a live post.",
            $"blocked:{blocked}|assisted:{assisted}");
    }

    private static async Task<(ObservationStatus, string, string, string, string?)> ActionsAsync(
        IAppDbContext db,
        Guid businessId,
        CancellationToken cancellationToken)
    {
        var failed = await db.WorkActions.CountAsync(a => a.BusinessId == businessId && a.Status == ActionStatus.Failed, cancellationToken);
        var escalated = await db.WorkActions.CountAsync(a => a.BusinessId == businessId && a.Status == ActionStatus.Escalated, cancellationToken);
        var assisted = await db.WorkActions.CountAsync(a => a.BusinessId == businessId && a.Status == ActionStatus.Assisted, cancellationToken);
        return (
            ObservationStatus.Observed,
            "Action failures",
            $"{failed} failed, {escalated} escalated, and {assisted} assisted action(s) are stored. Provider results were not invented.",
            failed + escalated > 0 ? "Open the action center to retry or escalate." : "No failed actions. Continue with assisted live writes.",
            $"failed:{failed}|escalated:{escalated}|assisted:{assisted}");
    }
}

public sealed class GetMonitoringWorkspaceHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetMonitoringWorkspaceHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<MonitoringWorkspaceResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var plan = await MonitoringStore.PlanAsync(_db, tenantId, cancellationToken);
        await MonitoringStore.EnsureScheduleAsync(_db, tenantId, businessId, MonitoringStore.IntervalHours(plan), cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await MonitoringStore.LoadAsync(_db, businessId, cancellationToken);
    }
}

public sealed class RunMonitoringHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IWebsiteFetcher _fetcher;
    private readonly IPlatformAdapterCatalog _adapters;

    public RunMonitoringHandler(IAppDbContext db, ITenantContext tenant, IWebsiteFetcher fetcher, IPlatformAdapterCatalog adapters)
    {
        _db = db;
        _tenant = tenant;
        _fetcher = fetcher;
        _adapters = adapters;
    }

    public async Task<MonitoringWorkspaceResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        return await MonitoringEngine.RunAsync(_db, _fetcher, _adapters, tenantId, businessId, MonitoringTrigger.Manual, cancellationToken);
    }
}

public sealed class AssemblePresenceReportHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public AssemblePresenceReportHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<MonitoringWorkspaceResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var plan = await MonitoringStore.PlanAsync(_db, tenantId, cancellationToken);
        await MonitoringStore.EnsureScheduleAsync(_db, tenantId, businessId, MonitoringStore.IntervalHours(plan), cancellationToken);
        return await MonitoringEngine.AssembleReportAsync(_db, tenantId, businessId, cancellationToken);
    }
}

public sealed class AddCompetitorHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public AddCompetitorHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<MonitoringWorkspaceResponse> Handle(Guid businessId, AddCompetitorRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var plan = await MonitoringStore.PlanAsync(_db, tenantId, cancellationToken);
        await MonitoringStore.EnsureScheduleAsync(_db, tenantId, businessId, MonitoringStore.IntervalHours(plan), cancellationToken);
        var exists = await _db.Competitors.AnyAsync(c => c.BusinessId == businessId && c.Name == request.Name.Trim(), cancellationToken);
        if (exists)
        {
            throw AppException.Validation("That competitor is already on the operator list.");
        }

        try
        {
            _db.Competitors.Add(Competitor.Create(tenantId, businessId, request.Name, request.Website, request.Notes));
        }
        catch (ArgumentException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MonitoringStore.LoadAsync(_db, businessId, cancellationToken);
    }
}

public sealed class AcknowledgeAlertHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public AcknowledgeAlertHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<MonitoringWorkspaceResponse> Handle(Guid businessId, Guid alertId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var alert = await _db.MonitoringAlerts.FirstOrDefaultAsync(a => a.Id == alertId && a.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Alert was not found.");
        try
        {
            alert.Acknowledge();
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MonitoringStore.LoadAsync(_db, businessId, cancellationToken);
    }
}

public sealed class RecordReportDecisionHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public RecordReportDecisionHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<MonitoringWorkspaceResponse> Handle(
        Guid businessId,
        Guid reportId,
        RecordReportDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var report = await _db.PresenceReports.FirstOrDefaultAsync(r => r.Id == reportId && r.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Report was not found.");
        try
        {
            report.RecordDecision(request.Decision);
        }
        catch (ArgumentException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await MonitoringStore.LoadAsync(_db, businessId, cancellationToken);
    }
}
