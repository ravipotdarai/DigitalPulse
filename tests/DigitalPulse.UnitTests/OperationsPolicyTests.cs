using DigitalPulse.Domain.Operations;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class OperationsPolicyTests
{
    [Fact]
    public void Rate_limit_trips_at_the_catalog_window()
    {
        Assert.False(OperationsPolicy.IsOverLimit(119));
        Assert.True(OperationsPolicy.IsOverLimit(120));
    }

    [Fact]
    public void Secrets_are_redacted_and_never_enter_a_manifest()
    {
        Assert.True(OperationsPolicy.LooksLikeSecret("Bearer abc"));
        Assert.Equal("[redacted]", OperationsPolicy.Redact("ApiKey=secret"));
        Assert.Throws<InvalidOperationException>(() =>
            BackupSnapshot.Capture(Guid.NewGuid(), "{\"token\":\"Bearer abc\"}"));
    }

    [Fact]
    public void Restore_stays_on_the_owning_tenant()
    {
        var tenant = Guid.NewGuid();
        var snapshot = BackupSnapshot.Capture(tenant, OperationsPolicy.Manifest(1, 1, 0, 0, 0));
        var ok = RestoreAttempt.Verify(tenant, snapshot);
        Assert.Equal(RestoreStatus.Verified, ok.Status);
        Assert.Contains("not configured", ok.HoldReason, StringComparison.OrdinalIgnoreCase);
        Assert.Throws<InvalidOperationException>(() => RestoreAttempt.Verify(Guid.NewGuid(), snapshot));
    }

    [Fact]
    public void Failover_and_advisories_stay_held()
    {
        var drill = DisasterDrill.Run(Guid.NewGuid(), DrillKind.Failover, "No standby.", false, OperationsPolicy.FailoverHold);
        Assert.Equal(DrillStatus.Held, drill.Status);
        var inventory = DependencyInventory.Record(Guid.NewGuid(), InventoryKind.Dependency, ["FluentValidation"], OperationsPolicy.AdvisoryHold);
        Assert.Equal(1, inventory.PackageCount);
        Assert.Contains("not invented", inventory.HoldReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Readiness_with_holds_is_not_production_ready()
    {
        var review = ReadinessReview.Assemble(Guid.NewGuid(), "Development", 4, 0, OperationsPolicy.ProductionNotReady);
        Assert.Equal(ReadinessStatus.ReadyWithHolds, review.Status);
    }
}
