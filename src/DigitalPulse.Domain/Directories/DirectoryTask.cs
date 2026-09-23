using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Directories;

public enum DirectoryTaskKind
{
    ProfileSync = 1
}

public enum DirectoryTaskStatus
{
    Prepared = 1,
    InProgress = 2,
    AwaitingVerification = 3,
    Verified = 4
}

public sealed class DirectoryTask : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public string PlatformCode { get; private set; } = string.Empty;
    public DirectoryTaskKind Kind { get; private set; } = DirectoryTaskKind.ProfileSync;
    public DirectoryTaskStatus Status { get; private set; } = DirectoryTaskStatus.Prepared;
    public string PreparedName { get; private set; } = string.Empty;
    public string? PreparedPhone { get; private set; }
    public string? PreparedWebsite { get; private set; }
    public string? PreparedCategory { get; private set; }
    public string? PreparedServices { get; private set; }
    public string? VerificationNote { get; private set; }
    public DateTimeOffset? VerifiedAtUtc { get; private set; }
    public DateTimeOffset? LastMonitoredAtUtc { get; private set; }
    public string? MonitorDetail { get; private set; }

    private readonly List<DirectoryStep> _steps = [];
    public IReadOnlyCollection<DirectoryStep> Steps => _steps;

    private DirectoryTask() { }

    public static DirectoryTask Prepare(
        Guid tenantId,
        Guid businessId,
        string platformCode,
        string preparedName,
        string? preparedPhone,
        string? preparedWebsite,
        string? preparedCategory,
        string? preparedServices,
        IReadOnlyList<(string Title, string Detail)> steps)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(platformCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(preparedName);
        if (steps.Count == 0) throw new ArgumentException("Assisted steps are required.", nameof(steps));

        var task = new DirectoryTask
        {
            TenantId = tenantId,
            BusinessId = businessId,
            PlatformCode = platformCode.Trim().ToUpperInvariant(),
            Kind = DirectoryTaskKind.ProfileSync,
            Status = DirectoryTaskStatus.Prepared,
            PreparedName = preparedName.Trim(),
            PreparedPhone = NullIfEmpty(preparedPhone),
            PreparedWebsite = NullIfEmpty(preparedWebsite),
            PreparedCategory = NullIfEmpty(preparedCategory),
            PreparedServices = NullIfEmpty(preparedServices)
        };

        var ordinal = 1;
        foreach (var (title, detail) in steps)
        {
            task._steps.Add(DirectoryStep.Create(tenantId, task.Id, ordinal++, title, detail));
        }

        return task;
    }

    public void CompleteStep(Guid stepId)
    {
        var step = _steps.FirstOrDefault(s => s.Id == stepId)
            ?? throw new InvalidOperationException("Step was not found.");
        step.Complete();
        Status = _steps.All(s => s.CompletedAtUtc is not null)
            ? DirectoryTaskStatus.AwaitingVerification
            : DirectoryTaskStatus.InProgress;
        Touch();
    }

    public void Verify(string note)
    {
        if (Status != DirectoryTaskStatus.AwaitingVerification)
        {
            throw new InvalidOperationException("Complete every assisted step before verifying.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(note);
        Status = DirectoryTaskStatus.Verified;
        VerificationNote = note.Trim();
        VerifiedAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void RecordMonitor(string detail)
    {
        LastMonitoredAtUtc = DateTimeOffset.UtcNow;
        MonitorDetail = detail.Trim();
        Touch();
    }

    public void AttachSteps(IEnumerable<DirectoryStep> steps)
    {
        _steps.Clear();
        _steps.AddRange(steps.OrderBy(s => s.Ordinal));
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class DirectoryStep : TenantOwnedEntity
{
    public Guid TaskId { get; private set; }
    public int Ordinal { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Detail { get; private set; } = string.Empty;
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    private DirectoryStep() { }

    public static DirectoryStep Create(Guid tenantId, Guid taskId, int ordinal, string title, string detail)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (taskId == Guid.Empty) throw new ArgumentException("Task is required.", nameof(taskId));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);
        if (ordinal < 1) throw new ArgumentOutOfRangeException(nameof(ordinal));

        return new DirectoryStep
        {
            TenantId = tenantId,
            TaskId = taskId,
            Ordinal = ordinal,
            Title = title.Trim(),
            Detail = detail.Trim()
        };
    }

    public void Complete()
    {
        CompletedAtUtc ??= DateTimeOffset.UtcNow;
        Touch();
    }
}
