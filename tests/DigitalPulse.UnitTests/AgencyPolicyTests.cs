using DigitalPulse.Domain.Agency;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Tenancy;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class AgencyPolicyTests
{
    [Fact]
    public void Direct_tenants_cannot_open_the_agency_workspace()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => AgencyPolicy.EnsureAgencyTenant(TenantType.Direct));
        Assert.Contains("Agency tenants", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void White_label_is_an_agency_entitlement()
    {
        var starter = SubscriptionPlan.Create("STARTER", "Starter", 2999m, 1, false, 1);
        var agency = SubscriptionPlan.Create("AGENCY", "Agency", 29999m, 100, true, 4, maxAgencyClients: 50, whiteLabel: true);
        Assert.Throws<InvalidOperationException>(() => AgencyPolicy.EnsureWhiteLabel(starter));
        AgencyPolicy.EnsureWhiteLabel(agency);
    }

    [Fact]
    public void Client_status_moves_forward_without_skipping_pause()
    {
        Assert.True(AgencyPolicy.CanTransition(AgencyClientStatus.Prospect, AgencyClientStatus.Active));
        Assert.True(AgencyPolicy.CanTransition(AgencyClientStatus.Active, AgencyClientStatus.Paused));
        Assert.False(AgencyPolicy.CanTransition(AgencyClientStatus.Active, AgencyClientStatus.Prospect));
    }

    [Fact]
    public void Custom_domain_stores_a_hostname_and_stays_held()
    {
        Assert.Equal("reports.agency.example", AgencyPolicy.NormalizeDomain("Reports.Agency.Example"));
        Assert.Throws<ArgumentException>(() => AgencyPolicy.NormalizeDomain("https://reports.agency.example"));
        var profile = WhiteLabelProfile.Create(Guid.NewGuid());
        profile.Apply("Northwind", "ops@agency.example", null, "#1A2B3C", null, "reports.agency.example", true);
        Assert.True(profile.Enabled);
        Assert.Equal("#1A2B3C", profile.PrimaryColor);
        Assert.Contains("not live", profile.HoldReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Client_report_must_name_one_business()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            AgencyReport.Assemble(Guid.NewGuid(), AgencyReportScope.Client, null, null, "Title", "Fact", "Rec", "AI", "Held."));
        Assert.Contains("client business", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Workflow_holds_instead_of_inventing_hosting()
    {
        var workflow = AgencyWorkflow.Start(Guid.NewGuid(), AgencyWorkflowKind.WhiteLabelReview, null, null);
        var steps = workflow.CreateSteps();
        workflow.Advance(steps, "Checked domain", hold: true, AgencyPolicy.CustomDomainHold);
        Assert.Equal(AgencyWorkflowStatus.Held, workflow.Status);
        Assert.Contains("not live", workflow.HoldReason, StringComparison.OrdinalIgnoreCase);
    }
}
