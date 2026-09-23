using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Features.Scans;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Platforms;
using DigitalPulse.Domain.Scans;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class DigitalPulseCheckTests
{
    [Fact]
    public void Thin_identity_produces_evidence_backed_gaps()
    {
        var business = Business.Create(Guid.NewGuid(), "Harbour Coffee", null);
        var findings = CheckComparisons.CompareIdentity(business, [], [], 0, 0, []);

        Assert.Contains(findings, f => f.Title.Contains("website", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(findings, f => f.Title.Contains("Phone", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(findings, f => f.Title.Contains("location", StringComparison.OrdinalIgnoreCase));
        Assert.All(findings, f => Assert.NotEmpty(f.Evidence));
    }

    [Fact]
    public void Restricted_facts_are_policy_findings_not_publishable_claims()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "Ledger Cafe", "https://ledger.example");
        var facts = new[]
        {
            BusinessFact.Create(tenantId, business.Id, "GSTIN", "27AAAAA0000A1Z5", FactStatus.Restricted)
        };

        var findings = CheckComparisons.CompareIdentity(business, [], [], 1, 1, facts);
        var restricted = Assert.Single(findings, f => f.Category == "Policy");
        Assert.Equal(FindingSeverity.Info, restricted.Severity);
        Assert.Contains("GSTIN", restricted.ObservedValue);
        Assert.DoesNotContain(findings, f => f.Title.Contains("Google", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Development_grant_does_not_invent_platform_listings()
    {
        var tenantId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var connection = PlatformConnection.Start(tenantId, businessId, "GOOGLE", PlatformAuthMode.OAuth, "state-1");
        connection.MarkConnected("Development", "dev-ref", "dev-account");

        var findings = CheckComparisons.CompareConnections([connection], new StubCatalog());
        var snapshot = Assert.Single(findings);
        Assert.Contains("unavailable", snapshot.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(FindingAutomationState.Blocked, snapshot.AutomationState);
        Assert.DoesNotContain(findings, f => f.ObservedValue?.Contains("4.8 stars", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(snapshot.Evidence, e => e.Value == "Development");
    }

    [Fact]
    public void Disabled_website_probe_adds_no_website_findings()
    {
        var business = Business.Create(Guid.NewGuid(), "Harbour Coffee", "https://harbour.example");
        var findings = CheckComparisons.CompareWebsite(business, WebsiteProbeResult.DisabledInHost(), []);
        Assert.Empty(findings);
    }

    [Fact]
    public void Blocked_website_is_a_high_finding()
    {
        var business = Business.Create(Guid.NewGuid(), "Harbour Coffee", "https://127.0.0.1");
        var findings = CheckComparisons.CompareWebsite(
            business,
            WebsiteProbeResult.BlockedUrl("Website host is not allowed."),
            []);
        Assert.Contains(findings, f => f.Severity == FindingSeverity.High && f.Category == "Website");
    }

    private sealed class StubCatalog : IPlatformAdapterCatalog
    {
        public IReadOnlyList<IPlatformAdapter> All() => [];
        public IPlatformAdapter Get(string code) => new StubAdapter(code);
    }

    private sealed class StubAdapter : IPlatformAdapter
    {
        private readonly string _code;
        public StubAdapter(string code) => _code = code;
        public PlatformDescriptor Describe() =>
            new(_code, "Google Business Profile", "Listings", PlatformAuthMode.OAuth, "Test", new PlatformCapabilities(true, false, false, false, false, false, false));
        public Task<PlatformHealthResult> HealthCheckAsync(PlatformConnection connection, CancellationToken cancellationToken) =>
            Task.FromResult(new PlatformHealthResult("Hold", "Test"));
        public Task<IReadOnlyList<PlatformDiagnostic>> DiagnoseAsync(PlatformConnection connection, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PlatformDiagnostic>>([]);
    }
}
