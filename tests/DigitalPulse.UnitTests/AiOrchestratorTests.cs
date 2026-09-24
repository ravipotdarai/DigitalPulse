using DigitalPulse.Domain.Ai;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class AiOrchestratorTests
{
    [Fact]
    public void No_evidence_rejects_any_factual_claim()
    {
        var result = AiPolicy.Evaluate([], "The cafe opened in 1994.", providerIsLive: true);
        Assert.False(result.Passed);
        Assert.Equal(AiRunStatus.Rejected, result.Status);
        Assert.Equal(AiConfidence.None, result.Confidence);
        Assert.Contains("No evidence", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Conflicting_approved_evidence_requires_review()
    {
        var evidence = new List<AiEvidence>
        {
            new("fact", "PHONE", "+91 22 0000 0001", false, true),
            new("fact", "PHONE", "+91 22 0000 0002", false, true)
        };

        var result = AiPolicy.Evaluate(evidence, "The phone is +91 22 0000 0001.", providerIsLive: true);
        Assert.False(result.Passed);
        Assert.True(result.HasConflict);
        Assert.Equal(AiRunStatus.NeedsReview, result.Status);
    }

    [Fact]
    public void Restricted_fact_in_output_never_passes()
    {
        var evidence = new List<AiEvidence>
        {
            new("fact", "REVENUE", "₹4.2 crore", true, false),
            new("fact", "NAME", "Harbour Roast", false, true)
        };

        var result = AiPolicy.Evaluate(evidence, "Harbour Roast made ₹4.2 crore last year.", providerIsLive: true);
        Assert.False(result.Passed);
        Assert.True(result.HasRestrictedFact);
        Assert.Equal(AiRunStatus.NeedsReview, result.Status);
        Assert.Contains("never be published", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Development_provider_holds_even_when_evidence_exists()
    {
        var evidence = new List<AiEvidence>
        {
            new("fact", "NAME", "Harbour Roast", false, true),
            new("knowledge", "Voice", "Warm and precise.", false, true)
        };

        var result = AiPolicy.Evaluate(evidence, "Harbour Roast is warm and precise.", providerIsLive: false);
        Assert.True(result.Passed);
        Assert.Equal(AiRunStatus.Held, result.Status);
        Assert.Equal(AiConfidence.Low, result.Confidence);
        Assert.Contains("live AI provider is not configured", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Live_provider_with_approved_evidence_completes_at_medium_or_high()
    {
        var evidence = new List<AiEvidence>
        {
            new("fact", "NAME", "Harbour Roast", false, true),
            new("fact", "CITY", "Mumbai", false, true),
            new("knowledge", "Offer", "Wholesale roast program.", false, true)
        };

        var result = AiPolicy.Evaluate(evidence, "Harbour Roast in Mumbai runs a wholesale roast program.", providerIsLive: true);
        Assert.True(result.Passed);
        Assert.Equal(AiRunStatus.Completed, result.Status);
        Assert.Equal(AiConfidence.High, result.Confidence);
    }

    [Fact]
    public void Catalog_exposes_every_specialized_agent()
    {
        Assert.Equal(10, AiAgentCatalog.All.Count);
        Assert.Contains(AiAgentCatalog.All, a => a.Code == "whatsapp");
        Assert.Contains(AiAgentCatalog.All, a => a.Code == "competitor");
        Assert.Equal(AiAgentKind.Content, AiAgentCatalog.Require("content").Kind);
    }
}
