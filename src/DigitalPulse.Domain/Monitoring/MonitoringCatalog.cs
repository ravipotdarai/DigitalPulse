using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Monitoring;

public enum MonitoringKind
{
    PlatformHealth = 0,
    WebsiteAvailability = 1,
    IdentityConsistency = 2,
    SearchVisibility = 3,
    SocialActivity = 4,
    WhatsAppHealth = 5,
    ReviewChanges = 6,
    ProfileChanges = 7,
    CompetitorChanges = 8,
    PublishedContent = 9,
    ActionFailures = 10,
    ApiFailures = 11
}

public enum MonitoringRunStatus
{
    Running = 0,
    Completed = 1,
    Failed = 2
}

public enum MonitoringTrigger
{
    Manual = 0,
    Scheduled = 1
}

public enum ObservationStatus
{
    Observed = 0,
    Held = 1,
    Changed = 2,
    Unchanged = 3
}

public enum AlertSeverity
{
    Info = 0,
    Warning = 1,
    High = 2
}

public enum AlertStatus
{
    Open = 0,
    Acknowledged = 1,
    Resolved = 2
}

public enum ReportKind
{
    Pulse = 0,
    Change = 1,
    Health = 2
}

public sealed record MonitoringKindDescriptor(MonitoringKind Kind, string Code, string Name, bool CanObserveWithoutLiveApi, string Purpose);

public static class MonitoringCatalog
{
    public static readonly IReadOnlyList<MonitoringKindDescriptor> All =
    [
        new(MonitoringKind.PlatformHealth, "platform-health", "Platform connection health", true, "Uses stored grants and adapter health. Live provider status is not invented."),
        new(MonitoringKind.WebsiteAvailability, "website-availability", "Website availability", true, "Safe homepage probe. Not a Search Console crawl."),
        new(MonitoringKind.IdentityConsistency, "identity-consistency", "Business identity consistency", true, "Compares the canonical record to the last stored snapshot."),
        new(MonitoringKind.SearchVisibility, "search-visibility", "Search visibility", false, "Search Console and ranking reads stay held without a live API."),
        new(MonitoringKind.SocialActivity, "social-activity", "Social activity", false, "Likes and views are not invented. Only stored drafts are observed."),
        new(MonitoringKind.WhatsAppHealth, "whatsapp-health", "WhatsApp health", true, "Uses stored Cloud API account, opt-outs, and held sends."),
        new(MonitoringKind.ReviewChanges, "review-changes", "Review changes", false, "Official review feeds are not invented."),
        new(MonitoringKind.ProfileChanges, "profile-changes", "Profile changes", true, "Detects changes in the stored identity fingerprint."),
        new(MonitoringKind.CompetitorChanges, "competitor-changes", "Competitor changes", false, "Competitor listings are operator-recorded. Official reads stay held."),
        new(MonitoringKind.PublishedContent, "published-content", "Published content", true, "Counts stored publish holds. Live posts are not invented."),
        new(MonitoringKind.ActionFailures, "action-failures", "Action failures", true, "Reads the action engine. No provider result is invented."),
        new(MonitoringKind.ApiFailures, "api-failures", "API failures", false, "Provider API failures are recorded only when a live adapter reports them.")
    ];
}

public static class MonitoringPolicy
{
    public static int IntervalHoursFor(string planCode) => planCode.ToUpperInvariant() switch
    {
        "STARTER" => 168,
        "GROWTH" => 24,
        "BUSINESS" => 6,
        "AGENCY" => 1,
        _ => 168
    };

    public static bool IsDue(DateTimeOffset? nextRunAtUtc, DateTimeOffset? now = null) =>
        nextRunAtUtc is null || (now ?? DateTimeOffset.UtcNow) >= nextRunAtUtc;
}

public sealed class MonitoringSchedule : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public int IntervalHours { get; private set; } = 168;
    public bool Enabled { get; private set; } = true;
    public DateTimeOffset? LastRunAtUtc { get; private set; }
    public DateTimeOffset? NextRunAtUtc { get; private set; }
    public string HoldReason { get; private set; } = "Scheduled monitoring waits for the first run. Live provider metrics stay held.";

    private MonitoringSchedule() { }

    public static MonitoringSchedule Create(Guid tenantId, Guid businessId, int intervalHours) =>
        new()
        {
            TenantId = tenantId,
            BusinessId = businessId,
            IntervalHours = intervalHours < 1 ? 168 : intervalHours,
            Enabled = true,
            NextRunAtUtc = DateTimeOffset.UtcNow,
            HoldReason = $"Plan interval is {intervalHours} hour(s). Live platform metrics are not invented."
        };

    public void SyncInterval(int intervalHours)
    {
        IntervalHours = intervalHours < 1 ? 168 : intervalHours;
        Touch();
    }

    public void MarkRan(DateTimeOffset atUtc, string detail)
    {
        LastRunAtUtc = atUtc;
        NextRunAtUtc = atUtc.AddHours(IntervalHours);
        HoldReason = detail.Trim();
        Touch();
    }
}

public sealed class MonitoringRun : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public MonitoringTrigger Trigger { get; private set; }
    public MonitoringRunStatus Status { get; private set; } = MonitoringRunStatus.Running;
    public string Summary { get; private set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    private MonitoringRun() { }

    public static MonitoringRun Start(Guid tenantId, Guid businessId, MonitoringTrigger trigger) =>
        new()
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Trigger = trigger,
            Status = MonitoringRunStatus.Running,
            StartedAtUtc = DateTimeOffset.UtcNow
        };

    public void Complete(string summary)
    {
        Status = MonitoringRunStatus.Completed;
        Summary = summary.Trim();
        CompletedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Fail(string error)
    {
        Status = MonitoringRunStatus.Failed;
        Summary = error.Trim();
        CompletedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }
}

public sealed class MonitoringResult : TenantOwnedEntity
{
    public Guid RunId { get; private set; }
    public Guid BusinessId { get; private set; }
    public MonitoringKind Kind { get; private set; }
    public ObservationStatus Status { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string ObservedFact { get; private set; } = string.Empty;
    public string Recommendation { get; private set; } = string.Empty;
    public string? PreviousValue { get; private set; }
    public string? CurrentValue { get; private set; }

    private MonitoringResult() { }

    public static MonitoringResult Record(
        Guid tenantId,
        Guid runId,
        Guid businessId,
        MonitoringKind kind,
        ObservationStatus status,
        string title,
        string observedFact,
        string recommendation,
        string? previousValue,
        string? currentValue) =>
        new()
        {
            TenantId = tenantId,
            RunId = runId,
            BusinessId = businessId,
            Kind = kind,
            Status = status,
            Title = title.Trim(),
            ObservedFact = observedFact.Trim(),
            Recommendation = recommendation.Trim(),
            PreviousValue = string.IsNullOrWhiteSpace(previousValue) ? null : previousValue.Trim(),
            CurrentValue = string.IsNullOrWhiteSpace(currentValue) ? null : currentValue.Trim()
        };
}

public sealed class MonitoringAlert : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public Guid? ResultId { get; private set; }
    public AlertSeverity Severity { get; private set; }
    public AlertStatus Status { get; private set; } = AlertStatus.Open;
    public string Title { get; private set; } = string.Empty;
    public string Detail { get; private set; } = string.Empty;
    public DateTimeOffset OpenedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AcknowledgedAtUtc { get; private set; }

    private MonitoringAlert() { }

    public static MonitoringAlert Open(
        Guid tenantId,
        Guid businessId,
        Guid? resultId,
        AlertSeverity severity,
        string title,
        string detail) =>
        new()
        {
            TenantId = tenantId,
            BusinessId = businessId,
            ResultId = resultId,
            Severity = severity,
            Title = title.Trim(),
            Detail = detail.Trim(),
            OpenedAtUtc = DateTimeOffset.UtcNow
        };

    public void Acknowledge()
    {
        if (Status != AlertStatus.Open)
        {
            throw new InvalidOperationException("Only open alerts can be acknowledged.");
        }

        Status = AlertStatus.Acknowledged;
        AcknowledgedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Resolve()
    {
        Status = AlertStatus.Resolved;
        Touch();
    }
}

public sealed class Competitor : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Website { get; private set; }
    public string? Notes { get; private set; }

    private Competitor() { }

    public static Competitor Create(Guid tenantId, Guid businessId, string name, string? website, string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Competitor
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Name = name.Trim(),
            Website = string.IsNullOrWhiteSpace(website) ? null : website.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }
}

public sealed class CompetitorObservation : TenantOwnedEntity
{
    public Guid CompetitorId { get; private set; }
    public Guid RunId { get; private set; }
    public ObservationStatus Status { get; private set; } = ObservationStatus.Held;
    public string Detail { get; private set; } = string.Empty;

    private CompetitorObservation() { }

    public static CompetitorObservation Hold(Guid tenantId, Guid competitorId, Guid runId, string detail) =>
        new()
        {
            TenantId = tenantId,
            CompetitorId = competitorId,
            RunId = runId,
            Status = ObservationStatus.Held,
            Detail = detail.Trim()
        };
}

public sealed class PresenceReport : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public ReportKind Kind { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string ObservedFact { get; private set; } = string.Empty;
    public string Recommendation { get; private set; } = string.Empty;
    public string AiInterpretation { get; private set; } = string.Empty;
    public string? CustomerDecision { get; private set; }
    public DateTimeOffset PeriodStartUtc { get; private set; }
    public DateTimeOffset PeriodEndUtc { get; private set; }
    public string HoldReason { get; private set; } = string.Empty;

    private PresenceReport() { }

    public static PresenceReport Assemble(
        Guid tenantId,
        Guid businessId,
        ReportKind kind,
        string title,
        string observedFact,
        string recommendation,
        string aiInterpretation,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        string holdReason) =>
        new()
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Kind = kind,
            Title = title.Trim(),
            ObservedFact = observedFact.Trim(),
            Recommendation = recommendation.Trim(),
            AiInterpretation = aiInterpretation.Trim(),
            PeriodStartUtc = periodStart,
            PeriodEndUtc = periodEnd,
            HoldReason = holdReason.Trim()
        };

    public void RecordDecision(string decision)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decision);
        CustomerDecision = decision.Trim();
        Touch();
    }
}
