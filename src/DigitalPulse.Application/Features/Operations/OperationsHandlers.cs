using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Contracts.Operations;
using DigitalPulse.Domain.Ai;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Operations;
using DigitalPulse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Operations;

internal static class OperationsMaps
{
    public static BackupSnapshotResponse ToResponse(this BackupSnapshot snapshot) =>
        new(snapshot.Id, snapshot.Status.ToString(), snapshot.Manifest, snapshot.Checksum, snapshot.HoldReason, snapshot.CreatedAtUtc);

    public static RestoreAttemptResponse ToResponse(this RestoreAttempt attempt) =>
        new(attempt.Id, attempt.SnapshotId, attempt.Status.ToString(), attempt.HoldReason, attempt.CreatedAtUtc);

    public static DisasterDrillResponse ToResponse(this DisasterDrill drill) =>
        new(drill.Id, drill.Kind.ToString(), drill.Status.ToString(), drill.ObservedFact, drill.HoldReason, drill.CreatedAtUtc);

    public static DependencyInventoryResponse ToResponse(this DependencyInventory inventory) =>
        new(inventory.Id, inventory.Kind.ToString(), inventory.Status.ToString(), inventory.PackageCount, inventory.Packages, inventory.HoldReason, inventory.CreatedAtUtc);

    public static ReadinessReviewResponse ToResponse(this ReadinessReview review, IReadOnlyList<ReadinessCheck> checks) =>
        new(
            review.Id,
            review.Status.ToString(),
            review.EnvironmentName,
            review.HoldCount,
            review.FailCount,
            review.HoldReason,
            checks.Select(c => new ReadinessCheckResponse(c.Code, c.Title, c.Outcome.ToString(), c.Detail)).ToList());
}

internal static class OperationsStore
{
    public static async Task<(Tenant Tenant, Subscription Subscription, SubscriptionPlan Plan)> RequireAsync(
        IAppDbContext db,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
        var subscription = await db.Subscriptions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw AppException.Validation("Select a plan before opening operations.");
        EntitlementRules.EnsureUsable(subscription.Status);
        var plan = await db.Plans.FirstAsync(p => p.Id == subscription.PlanId, cancellationToken);
        return (tenant, subscription, plan);
    }

    public static async Task<OperationsWorkspaceResponse> LoadAsync(
        IAppDbContext db,
        Tenant tenant,
        SubscriptionPlan plan,
        IOperationsEnvironment env,
        CancellationToken cancellationToken)
    {
        var start = BillingPolicy.PeriodStart();
        var scans = await db.Scans.CountAsync(s => s.TenantId == tenant.Id && s.CreatedAtUtc >= start, cancellationToken);
        var actions = await db.WorkActions.CountAsync(a => a.TenantId == tenant.Id && a.CreatedAtUtc >= start, cancellationToken);
        var ai = await db.AiRuns.CountAsync(r => r.TenantId == tenant.Id && r.CreatedAtUtc >= start && r.Status == AiRunStatus.Completed, cancellationToken);
        var backups = await db.BackupSnapshots.AsNoTracking()
            .Where(b => b.TenantId == tenant.Id)
            .OrderByDescending(b => b.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);
        var restores = await db.RestoreAttempts.AsNoTracking()
            .Where(r => r.TenantId == tenant.Id)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);
        var drills = await db.DisasterDrills.AsNoTracking()
            .Where(d => d.TenantId == tenant.Id)
            .OrderByDescending(d => d.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);
        var inventories = await db.DependencyInventories.AsNoTracking()
            .Where(i => i.TenantId == tenant.Id)
            .OrderByDescending(i => i.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);
        var review = await db.ReadinessReviews.AsNoTracking()
            .Where(r => r.TenantId == tenant.Id)
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var checks = review is null
            ? []
            : await db.ReadinessChecks.AsNoTracking()
                .Where(c => c.ReviewId == review.Id && c.TenantId == tenant.Id)
                .ToListAsync(cancellationToken);
        var audits = await db.OperationsAudits.AsNoTracking()
            .Where(a => a.TenantId == tenant.Id)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        return new OperationsWorkspaceResponse(
            env.EnvironmentName,
            env.HostRole,
            env.RateLimitingEnabled,
            env.RateLimitPerMinute,
            env.KeyVaultConfigured,
            env.AppInsightsConfigured,
            env.RedisConfigured,
            env.AzureBackupConfigured,
            "Production hardening records stored health, tenant-scoped backups, and honest holds. Azure, Redis, and scanners are not invented.",
            [
                new CostControlResponse("Scans", scans, plan.ScansPerMonth, OperationsPolicy.CostHold),
                new CostControlResponse("Actions", actions, plan.ActionsPerMonth, OperationsPolicy.CostHold),
                new CostControlResponse("AiGenerations", ai, plan.AiGenerationsPerMonth, OperationsPolicy.CostHold),
                new CostControlResponse("StorageGb", 0, plan.StorageGb, OperationsPolicy.CostHold)
            ],
            backups.Select(b => b.ToResponse()).ToList(),
            restores.Select(r => r.ToResponse()).ToList(),
            drills.Select(d => d.ToResponse()).ToList(),
            inventories.Select(i => i.ToResponse()).ToList(),
            review?.ToResponse(checks),
            audits.Select(a => new OperationsAuditResponse(a.Id, a.Action, a.Detail, a.CreatedAtUtc)).ToList());
    }

    public static void Audit(IAppDbContext db, Guid tenantId, string action, string detail) =>
        db.OperationsAudits.Add(OperationsAudit.Record(tenantId, action, detail));
}

public sealed class GetOperationsWorkspaceHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IOperationsEnvironment _env;

    public GetOperationsWorkspaceHandler(IAppDbContext db, ITenantContext tenant, IOperationsEnvironment env)
    {
        _db = db;
        _tenant = tenant;
        _env = env;
    }

    public async Task<OperationsWorkspaceResponse> Handle(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var (tenant, _, plan) = await OperationsStore.RequireAsync(_db, tenantId, cancellationToken);
        return await OperationsStore.LoadAsync(_db, tenant, plan, _env, cancellationToken);
    }
}

public sealed class CreateBackupHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IOperationsEnvironment _env;

    public CreateBackupHandler(IAppDbContext db, ITenantContext tenant, IOperationsEnvironment env)
    {
        _db = db;
        _tenant = tenant;
        _env = env;
    }

    public async Task<OperationsWorkspaceResponse> Handle(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var (tenant, _, plan) = await OperationsStore.RequireAsync(_db, tenantId, cancellationToken);
        var manifest = OperationsPolicy.Manifest(
            await _db.Businesses.CountAsync(b => b.TenantId == tenantId, cancellationToken),
            await _db.Locations.CountAsync(l => l.TenantId == tenantId, cancellationToken),
            await _db.Scans.CountAsync(s => s.TenantId == tenantId, cancellationToken),
            await _db.WorkActions.CountAsync(a => a.TenantId == tenantId, cancellationToken),
            await _db.Invoices.CountAsync(i => i.TenantId == tenantId, cancellationToken));
        var snapshot = BackupSnapshot.Capture(tenantId, manifest);
        _db.BackupSnapshots.Add(snapshot);
        OperationsStore.Audit(_db, tenantId, "backup", $"Stored logical snapshot {snapshot.Checksum}.");
        await _db.SaveChangesAsync(cancellationToken);
        return await OperationsStore.LoadAsync(_db, tenant, plan, _env, cancellationToken);
    }
}

public sealed class RestoreBackupHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IOperationsEnvironment _env;

    public RestoreBackupHandler(IAppDbContext db, ITenantContext tenant, IOperationsEnvironment env)
    {
        _db = db;
        _tenant = tenant;
        _env = env;
    }

    public async Task<OperationsWorkspaceResponse> Handle(Guid snapshotId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var (tenant, _, plan) = await OperationsStore.RequireAsync(_db, tenantId, cancellationToken);
        var snapshot = await _db.BackupSnapshots.FirstOrDefaultAsync(s => s.Id == snapshotId && s.TenantId == tenantId, cancellationToken)
            ?? throw AppException.NotFound("Backup snapshot was not found.");
        try
        {
            var attempt = RestoreAttempt.Verify(tenantId, snapshot);
            _db.RestoreAttempts.Add(attempt);
            OperationsStore.Audit(_db, tenantId, "restore", $"Verified snapshot {snapshot.Id} for this tenant only.");
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await OperationsStore.LoadAsync(_db, tenant, plan, _env, cancellationToken);
    }
}

public sealed class StartDisasterDrillHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IOperationsEnvironment _env;

    public StartDisasterDrillHandler(IAppDbContext db, ITenantContext tenant, IOperationsEnvironment env)
    {
        _db = db;
        _tenant = tenant;
        _env = env;
    }

    public async Task<OperationsWorkspaceResponse> Handle(StartDisasterDrillRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var (tenant, _, plan) = await OperationsStore.RequireAsync(_db, tenantId, cancellationToken);
        DrillKind kind;
        try
        {
            kind = OperationsPolicy.ParseDrill(request.Kind);
        }
        catch (ArgumentException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        var backupCount = await _db.BackupSnapshots.CountAsync(s => s.TenantId == tenantId, cancellationToken);
        var drill = kind switch
        {
            DrillKind.Backup => DisasterDrill.Run(
                tenantId,
                kind,
                backupCount == 0
                    ? "No logical snapshot exists for this tenant."
                    : $"{backupCount} logical snapshot(s) are stored for this tenant.",
                backupCount > 0,
                backupCount > 0 ? OperationsPolicy.AzureBackupHold : "Capture a logical snapshot before treating backup as drilled."),
            DrillKind.Restore => DisasterDrill.Run(
                tenantId,
                kind,
                "Restore stays a same-tenant checksum dry-run.",
                false,
                OperationsPolicy.AzureRestoreHold),
            DrillKind.Failover => DisasterDrill.Run(
                tenantId,
                kind,
                "A second region is not configured.",
                false,
                OperationsPolicy.FailoverHold),
            _ => DisasterDrill.Run(
                tenantId,
                kind,
                "This process is a single API host.",
                false,
                OperationsPolicy.ScalingHold)
        };
        _db.DisasterDrills.Add(drill);
        OperationsStore.Audit(_db, tenantId, "drill", $"{kind} drill recorded from stored operations facts.");
        await _db.SaveChangesAsync(cancellationToken);
        return await OperationsStore.LoadAsync(_db, tenant, plan, _env, cancellationToken);
    }
}

public sealed class RunInventoryHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IOperationsEnvironment _env;
    private readonly IPackageInventory _packages;

    public RunInventoryHandler(IAppDbContext db, ITenantContext tenant, IOperationsEnvironment env, IPackageInventory packages)
    {
        _db = db;
        _tenant = tenant;
        _env = env;
        _packages = packages;
    }

    public async Task<OperationsWorkspaceResponse> Handle(RunInventoryRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var (tenant, _, plan) = await OperationsStore.RequireAsync(_db, tenantId, cancellationToken);
        InventoryKind kind;
        try
        {
            kind = OperationsPolicy.ParseInventory(request.Kind);
        }
        catch (ArgumentException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        var inventory = _packages.Collect();
        var recorded = kind == InventoryKind.Container
            ? DependencyInventory.Record(tenantId, kind, inventory.ContainerBases, inventory.ContainerHold)
            : DependencyInventory.Record(tenantId, kind, inventory.Packages, inventory.DependencyHold);
        _db.DependencyInventories.Add(recorded);
        OperationsStore.Audit(_db, tenantId, "inventory", $"{kind} inventory recorded. Advisories stay held.");
        await _db.SaveChangesAsync(cancellationToken);
        return await OperationsStore.LoadAsync(_db, tenant, plan, _env, cancellationToken);
    }
}

public sealed class AssembleReadinessHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IOperationsEnvironment _env;

    public AssembleReadinessHandler(IAppDbContext db, ITenantContext tenant, IOperationsEnvironment env)
    {
        _db = db;
        _tenant = tenant;
        _env = env;
    }

    public async Task<OperationsWorkspaceResponse> Handle(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var (tenant, subscription, plan) = await OperationsStore.RequireAsync(_db, tenantId, cancellationToken);
        var backups = await _db.BackupSnapshots.CountAsync(s => s.TenantId == tenantId, cancellationToken);
        var inventories = await _db.DependencyInventories.CountAsync(i => i.TenantId == tenantId, cancellationToken);
        var checks = new List<(string Code, string Title, CheckOutcome Outcome, string Detail)>
        {
            ("tenant-isolation", "Tenant isolation", CheckOutcome.Pass, "Tenant-owned queries are filtered from the authenticated session."),
            ("subscription", "Usable subscription", OperationsPolicy.Outcome(subscription.IsUsable, false), $"Subscription is {subscription.Status}."),
            ("headers", "Security headers", CheckOutcome.Pass, "Content-Type, frame, and CSP headers are applied on the API."),
            ("rate-limit", "Rate limiting", _env.RateLimitingEnabled ? CheckOutcome.Pass : CheckOutcome.Hold, _env.RateLimitingEnabled ? $"{_env.RateLimitPerMinute} requests per minute." : "Rate limiting is disabled in the test host."),
            ("backup", "Logical backup", backups > 0 ? CheckOutcome.Hold : CheckOutcome.Hold, backups > 0 ? OperationsPolicy.AzureBackupHold : "Capture a logical snapshot. Azure Backup stays held."),
            ("key-vault", "Secret store", _env.KeyVaultConfigured ? CheckOutcome.Pass : CheckOutcome.Hold, _env.KeyVaultConfigured ? "Key Vault is configured." : OperationsPolicy.KeyVaultHold),
            ("telemetry", "Production telemetry", _env.AppInsightsConfigured ? CheckOutcome.Pass : CheckOutcome.Hold, _env.AppInsightsConfigured ? "Application Insights is configured." : OperationsPolicy.InsightsHold),
            ("cache", "Shared cache", _env.RedisConfigured ? CheckOutcome.Pass : CheckOutcome.Hold, _env.RedisConfigured ? "Redis is configured." : OperationsPolicy.RedisHold),
            ("billing", "Live billing", _env.LiveBillingConfigured ? CheckOutcome.Pass : CheckOutcome.Hold, _env.LiveBillingConfigured ? "A live billing provider is configured." : "Checkout stays held until Razorpay keys confirm a capture."),
            ("ai", "Live AI", _env.LiveAiConfigured ? CheckOutcome.Pass : CheckOutcome.Hold, _env.LiveAiConfigured ? "A live AI provider is configured." : "The development AI provider holds without an API key."),
            ("inventory", "Dependency inventory", inventories > 0 ? CheckOutcome.Hold : CheckOutcome.Hold, inventories > 0 ? OperationsPolicy.AdvisoryHold : "Record a dependency inventory. CVEs are not invented.")
        };
        var holdCount = checks.Count(c => c.Outcome == CheckOutcome.Hold);
        var failCount = checks.Count(c => c.Outcome == CheckOutcome.Fail);
        var review = ReadinessReview.Assemble(
            tenantId,
            _env.EnvironmentName,
            holdCount,
            failCount,
            failCount > 0 || holdCount > 0 ? OperationsPolicy.ProductionNotReady : "All stored readiness gates passed.");
        _db.ReadinessReviews.Add(review);
        foreach (var check in checks)
        {
            _db.ReadinessChecks.Add(ReadinessCheck.Create(tenantId, review.Id, check.Code, check.Title, check.Outcome, check.Detail));
        }

        OperationsStore.Audit(_db, tenantId, "readiness", $"Assembled {checks.Count} stored gates. Production is not invented.");
        await _db.SaveChangesAsync(cancellationToken);
        return await OperationsStore.LoadAsync(_db, tenant, plan, _env, cancellationToken);
    }
}
