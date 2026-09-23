using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Scans;

public enum ScanStatus
{
    Running = 1,
    Completed = 2,
    Failed = 3
}

public enum ScanTrigger
{
    Manual = 1
}

public enum FindingSeverity
{
    Info = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum FindingStatus
{
    Open = 1,
    Acknowledged = 2,
    Resolved = 3
}

public enum FindingAutomationState
{
    None = 0,
    Suggested = 1,
    Assisted = 2,
    Blocked = 3
}

public enum EvidenceKind
{
    Identity = 1,
    Website = 2,
    Connection = 3,
    Policy = 4,
    Scan = 5
}

public sealed class Scan : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public ScanTrigger Trigger { get; private set; } = ScanTrigger.Manual;
    public ScanStatus Status { get; private set; } = ScanStatus.Running;
    public DateTimeOffset StartedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public string? Summary { get; private set; }
    public string? Error { get; private set; }

    private Scan() { }

    public static Scan Start(Guid tenantId, Guid businessId, ScanTrigger trigger = ScanTrigger.Manual)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        return new Scan
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Trigger = trigger,
            Status = ScanStatus.Running,
            StartedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void Complete(string summary)
    {
        Status = ScanStatus.Completed;
        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
        Error = null;
        CompletedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Fail(string error)
    {
        Status = ScanStatus.Failed;
        Error = string.IsNullOrWhiteSpace(error) ? "Scan failed." : error.Trim();
        CompletedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }
}

public sealed class Finding : TenantOwnedEntity
{
    public Guid ScanId { get; private set; }
    public Guid BusinessId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public FindingSeverity Severity { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? ExpectedValue { get; private set; }
    public string? ObservedValue { get; private set; }
    public string Recommendation { get; private set; } = string.Empty;
    public string SuggestedAction { get; private set; } = string.Empty;
    public string VerificationMethod { get; private set; } = string.Empty;
    public FindingAutomationState AutomationState { get; private set; }
    public FindingStatus Status { get; private set; } = FindingStatus.Open;

    private Finding() { }

    public static Finding Open(
        Guid tenantId,
        Guid scanId,
        Guid businessId,
        string category,
        FindingSeverity severity,
        string title,
        string description,
        string? expectedValue,
        string? observedValue,
        string recommendation,
        string suggestedAction,
        string verificationMethod,
        FindingAutomationState automationState)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (scanId == Guid.Empty) throw new ArgumentException("Scan is required.", nameof(scanId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new Finding
        {
            TenantId = tenantId,
            ScanId = scanId,
            BusinessId = businessId,
            Category = category.Trim(),
            Severity = severity,
            Title = title.Trim(),
            Description = description.Trim(),
            ExpectedValue = NullIfEmpty(expectedValue),
            ObservedValue = NullIfEmpty(observedValue),
            Recommendation = recommendation.Trim(),
            SuggestedAction = suggestedAction.Trim(),
            VerificationMethod = verificationMethod.Trim(),
            AutomationState = automationState,
            Status = FindingStatus.Open
        };
    }

    public void SetStatus(FindingStatus status)
    {
        Status = status;
        Touch();
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class FindingEvidence : TenantOwnedEntity
{
    public Guid FindingId { get; private set; }
    public EvidenceKind Kind { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;

    private FindingEvidence() { }

    public static FindingEvidence Create(
        Guid tenantId,
        Guid findingId,
        EvidenceKind kind,
        string label,
        string value,
        string source)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (findingId == Guid.Empty) throw new ArgumentException("Finding is required.", nameof(findingId));
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        return new FindingEvidence
        {
            TenantId = tenantId,
            FindingId = findingId,
            Kind = kind,
            Label = label.Trim(),
            Value = value.Trim(),
            Source = source.Trim()
        };
    }
}
