using DigitalPulse.Domain.Actions;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class ActionPolicyTests
{
    [Fact]
    public void High_risk_change_requires_approval_even_in_full_auto()
    {
        var decision = ActionPolicy.Evaluate(ActionKind.PublishSocial, AutomationMode.FullAuto, liveWriteAvailable: true, alreadyApproved: false);
        Assert.True(decision.RequiresApproval);
        Assert.False(decision.EligibleForAutopilot);
        Assert.Equal(ActionRisk.High, decision.Risk);
        Assert.Contains("approval", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void External_write_without_a_live_adapter_is_never_autopilot()
    {
        var decision = ActionPolicy.Evaluate(ActionKind.PublishSocial, AutomationMode.FullAuto, liveWriteAvailable: false, alreadyApproved: true);
        Assert.False(decision.EligibleForAutopilot);
        Assert.Contains("will not invent", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Full_auto_low_risk_internal_work_is_eligible()
    {
        var decision = ActionPolicy.Evaluate(ActionKind.RebuildGraphify, AutomationMode.FullAuto, liveWriteAvailable: false, alreadyApproved: false);
        Assert.True(decision.EligibleForAutopilot);
        Assert.False(decision.RequiresApproval);
        Assert.Equal(ActionRisk.Low, decision.Risk);
    }

    [Fact]
    public void Assisted_mode_keeps_low_risk_work_on_approval()
    {
        var decision = ActionPolicy.Evaluate(ActionKind.AnalyzeWebsite, AutomationMode.Assisted, liveWriteAvailable: false, alreadyApproved: false);
        Assert.True(decision.RequiresApproval);
        Assert.False(decision.EligibleForAutopilot);
    }

    [Fact]
    public void Enqueue_stores_a_stable_kind_and_target_key()
    {
        var kind = ActionKindCatalog.Of(ActionKind.RunScan);
        var decision = ActionPolicy.Evaluate(kind.Kind, AutomationMode.Assisted, false, false);
        var tenantId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var first = WorkAction.Enqueue(tenantId, businessId, kind, "First scan", "run-scan:workspace", null, null, false, decision);
        var second = WorkAction.Enqueue(tenantId, businessId, kind, "Second scan", "run-scan:workspace", null, null, false, decision);
        Assert.Equal(first.IdempotencyKey, second.IdempotencyKey);
        Assert.Equal(ActionStatus.PendingApproval, first.Status);
        Assert.False(first.AutopilotEligible);
    }

    [Fact]
    public void Full_auto_monitoring_is_eligible()
    {
        var decision = ActionPolicy.Evaluate(ActionKind.RunMonitoring, AutomationMode.FullAuto, liveWriteAvailable: false, alreadyApproved: false);
        Assert.True(decision.EligibleForAutopilot);
        Assert.False(decision.RequiresApproval);
        Assert.Equal(ActionRisk.Low, decision.Risk);
    }

    [Fact]
    public void Full_auto_enqueue_queues_low_risk_work()
    {
        var kind = ActionKindCatalog.Of(ActionKind.RebuildGraphify);
        var decision = ActionPolicy.Evaluate(kind.Kind, AutomationMode.FullAuto, false, false);
        var action = WorkAction.Enqueue(Guid.NewGuid(), Guid.NewGuid(), kind, "Rebuild graph", "rebuild-graphify:workspace", null, null, false, decision);
        Assert.Equal(ActionStatus.Queued, action.Status);
        Assert.True(action.AutopilotEligible);
    }
}
