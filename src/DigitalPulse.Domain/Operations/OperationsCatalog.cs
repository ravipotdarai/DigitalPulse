using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Operations;

public enum BackupStatus
{
    Held = 0,
    Completed = 1,
    Verified = 2,
    Failed = 3
}

public enum RestoreStatus
{
    Held = 0,
    Verified = 1,
    Failed = 2
}

public enum DrillKind
{
    Backup = 0,
    Restore = 1,
    Failover = 2,
    Scaling = 3
}

public enum DrillStatus
{
    Held = 0,
    Passed = 1,
    Failed = 2
}

public enum InventoryKind
{
    Dependency = 0,
    Container = 1
}

public enum InventoryStatus
{
    Held = 0,
    Recorded = 1
}

public enum CheckOutcome
{
    Pass = 0,
    Hold = 1,
    Fail = 2
}

public enum ReadinessStatus
{
    NotReady = 0,
    ReadyWithHolds = 1,
    Ready = 2
}

public static class OperationsPolicy
{
    public const int RateLimitPerMinute = 120;
    public const string AzureBackupHold =
        "Logical tenant snapshot stored. Azure Backup and geo-replicate are not configured and are not invented.";
    public const string AzureRestoreHold =
        "Restore is a same-tenant dry-run of the stored snapshot. A live Azure restore is not configured.";
    public const string FailoverHold =
        "Failover to a second region is not configured. DigitalPulse will not invent a healthy standby.";
    public const string ScalingHold =
        "Horizontal scale is not invented. This process is a single API host.";
    public const string AdvisoryHold =
        "Package inventory was recorded from project files. A live advisory feed is not configured, so CVEs are not invented.";
    public const string ContainerHold =
        "Container base images were inventoried from the Dockerfile. A live image scanner is not configured.";
    public const string KeyVaultHold =
        "Azure Key Vault is not configured. Development secrets stay in environment or user-secrets and are not logged.";
    public const string InsightsHold =
        "Application Insights is not configured. Correlation ids and structured logs stay in-process.";
    public const string RedisHold =
        "Redis is not configured. Responses are not served from an invented cache.";
    public const string CostHold =
        "Live Azure spend is not inventoried. Catalog entitlements are the cost budget.";
    public const string ProductionNotReady =
        "Production readiness stays held until Key Vault, a live backup target, and a live telemetry sink are configured.";

    public static bool IsOverLimit(int requestsInWindow, int limit = RateLimitPerMinute) =>
        requestsInWindow >= limit;

    public static bool LooksLikeSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("Bearer ", StringComparison.OrdinalIgnoreCase)
            || value.Contains("api-key", StringComparison.OrdinalIgnoreCase)
            || value.Contains("ApiKey", StringComparison.Ordinal)
            || value.Contains("KeySecret", StringComparison.Ordinal)
            || value.Contains("AccessToken", StringComparison.Ordinal)
            || value.Contains("SigningKey", StringComparison.Ordinal);
    }

    public static string Redact(string? value) => LooksLikeSecret(value) ? "[redacted]" : (value ?? string.Empty);

    public static string Manifest(int businesses, int locations, int scans, int actions, int invoices) =>
        $"{{\"businesses\":{businesses},\"locations\":{locations},\"scans\":{scans},\"actions\":{actions},\"invoices\":{invoices}}}";

    public static string Checksum(Guid tenantId, string manifest) =>
        $"{tenantId:N}:{manifest.Length}:{manifest.GetHashCode(StringComparison.Ordinal):X8}";

    public static DrillKind ParseDrill(string? value)
    {
        if (Enum.TryParse<DrillKind>(value, true, out var kind))
        {
            return kind;
        }

        throw new ArgumentException("Drill kind must be Backup, Restore, Failover, or Scaling.");
    }

    public static InventoryKind ParseInventory(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return InventoryKind.Dependency;
        }

        if (Enum.TryParse<InventoryKind>(value, true, out var kind))
        {
            return kind;
        }

        throw new ArgumentException("Scan kind must be Dependency or Container.");
    }

    public static ReadinessStatus StatusFor(int holdCount, int failCount) =>
        failCount > 0 || holdCount > 0
            ? (failCount > 0 ? ReadinessStatus.NotReady : ReadinessStatus.ReadyWithHolds)
            : ReadinessStatus.Ready;

    public static CheckOutcome Outcome(bool pass, bool hold) =>
        !pass ? CheckOutcome.Fail : hold ? CheckOutcome.Hold : CheckOutcome.Pass;
}

public sealed class BackupSnapshot : TenantOwnedEntity
{
    public BackupStatus Status { get; private set; } = BackupStatus.Held;
    public string Manifest { get; private set; } = string.Empty;
    public string Checksum { get; private set; } = string.Empty;
    public string HoldReason { get; private set; } = OperationsPolicy.AzureBackupHold;

    private BackupSnapshot() { }

    public static BackupSnapshot Capture(Guid tenantId, string manifest)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(manifest);
        if (OperationsPolicy.LooksLikeSecret(manifest))
        {
            throw new InvalidOperationException("Backup manifests cannot include secrets or tokens.");
        }

        return new BackupSnapshot
        {
            TenantId = tenantId,
            Status = BackupStatus.Completed,
            Manifest = manifest.Trim(),
            Checksum = OperationsPolicy.Checksum(tenantId, manifest.Trim()),
            HoldReason = OperationsPolicy.AzureBackupHold
        };
    }
}

public sealed class RestoreAttempt : TenantOwnedEntity
{
    public Guid SnapshotId { get; private set; }
    public RestoreStatus Status { get; private set; } = RestoreStatus.Held;
    public string HoldReason { get; private set; } = OperationsPolicy.AzureRestoreHold;

    private RestoreAttempt() { }

    public static RestoreAttempt Verify(Guid tenantId, BackupSnapshot snapshot)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (snapshot.TenantId != tenantId)
        {
            throw new InvalidOperationException("A restore can only target the tenant that owns the snapshot.");
        }

        var expected = OperationsPolicy.Checksum(tenantId, snapshot.Manifest);
        var ok = string.Equals(expected, snapshot.Checksum, StringComparison.Ordinal);
        return new RestoreAttempt
        {
            TenantId = tenantId,
            SnapshotId = snapshot.Id,
            Status = ok ? RestoreStatus.Verified : RestoreStatus.Failed,
            HoldReason = ok ? OperationsPolicy.AzureRestoreHold : "Snapshot checksum did not match the tenant manifest."
        };
    }
}

public sealed class DisasterDrill : TenantOwnedEntity
{
    public DrillKind Kind { get; private set; }
    public DrillStatus Status { get; private set; } = DrillStatus.Held;
    public string ObservedFact { get; private set; } = string.Empty;
    public string HoldReason { get; private set; } = string.Empty;

    private DisasterDrill() { }

    public static DisasterDrill Run(Guid tenantId, DrillKind kind, string observedFact, bool passed, string holdReason)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(observedFact);
        return new DisasterDrill
        {
            TenantId = tenantId,
            Kind = kind,
            Status = passed ? DrillStatus.Passed : DrillStatus.Held,
            ObservedFact = observedFact.Trim(),
            HoldReason = holdReason.Trim()
        };
    }
}

public sealed class DependencyInventory : TenantOwnedEntity
{
    public InventoryKind Kind { get; private set; }
    public InventoryStatus Status { get; private set; } = InventoryStatus.Held;
    public string Packages { get; private set; } = string.Empty;
    public int PackageCount { get; private set; }
    public string HoldReason { get; private set; } = OperationsPolicy.AdvisoryHold;

    private DependencyInventory() { }

    public static DependencyInventory Record(Guid tenantId, InventoryKind kind, IReadOnlyList<string> packages, string holdReason)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        var names = packages
            .Where(p => !string.IsNullOrWhiteSpace(p) && !OperationsPolicy.LooksLikeSecret(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var joined = string.Join(", ", names);
        if (joined.Length > 2000)
        {
            joined = joined[..2000];
        }

        return new DependencyInventory
        {
            TenantId = tenantId,
            Kind = kind,
            Status = names.Count == 0 ? InventoryStatus.Held : InventoryStatus.Recorded,
            Packages = joined,
            PackageCount = names.Count,
            HoldReason = string.IsNullOrWhiteSpace(holdReason) ? OperationsPolicy.AdvisoryHold : holdReason.Trim()
        };
    }
}

public sealed class ReadinessReview : TenantOwnedEntity
{
    public ReadinessStatus Status { get; private set; } = ReadinessStatus.NotReady;
    public int HoldCount { get; private set; }
    public int FailCount { get; private set; }
    public string EnvironmentName { get; private set; } = "Development";
    public string HoldReason { get; private set; } = OperationsPolicy.ProductionNotReady;

    private ReadinessReview() { }

    public static ReadinessReview Assemble(
        Guid tenantId,
        string environmentName,
        int holdCount,
        int failCount,
        string holdReason)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        return new ReadinessReview
        {
            TenantId = tenantId,
            EnvironmentName = string.IsNullOrWhiteSpace(environmentName) ? "Development" : environmentName.Trim(),
            HoldCount = holdCount,
            FailCount = failCount,
            Status = OperationsPolicy.StatusFor(holdCount, failCount),
            HoldReason = string.IsNullOrWhiteSpace(holdReason) ? OperationsPolicy.ProductionNotReady : holdReason.Trim()
        };
    }
}

public sealed class ReadinessCheck : TenantOwnedEntity
{
    public Guid ReviewId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public CheckOutcome Outcome { get; private set; }
    public string Detail { get; private set; } = string.Empty;

    private ReadinessCheck() { }

    public static ReadinessCheck Create(
        Guid tenantId,
        Guid reviewId,
        string code,
        string title,
        CheckOutcome outcome,
        string detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        return new ReadinessCheck
        {
            TenantId = tenantId,
            ReviewId = reviewId,
            Code = code.Trim(),
            Title = title.Trim(),
            Outcome = outcome,
            Detail = detail.Trim()
        };
    }
}

public sealed class OperationsAudit : TenantOwnedEntity
{
    public string Action { get; private set; } = string.Empty;
    public string Detail { get; private set; } = string.Empty;

    private OperationsAudit() { }

    public static OperationsAudit Record(Guid tenantId, string action, string detail)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        if (OperationsPolicy.LooksLikeSecret(detail))
        {
            detail = "[redacted]";
        }

        return new OperationsAudit
        {
            TenantId = tenantId,
            Action = action.Trim(),
            Detail = (detail ?? string.Empty).Trim()
        };
    }
}
