using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Ai;
using DigitalPulse.Domain.Ai;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class AiUsagePolicyTests
{
    [Fact]
    public void Cost_estimate_is_zero_until_official_rates_are_configured()
    {
        Assert.Equal(0, AiUsagePolicy.EstimateCostCents(1200, 400, 0, 0));
        Assert.Equal(4, AiUsagePolicy.EstimateCostCents(1000, 1000, 2, 2));
    }

    [Fact]
    public async Task Identical_asks_reuse_the_cached_orchestration()
    {
        var inner = new CountingOrchestrator();
        IAiOrchestrator orchestrator = new CachingAiOrchestrator(inner, new MemoryCache(new MemoryCacheOptions()));
        var request = new AiOrchestrationRequest(
            "content",
            "Write a project outline",
            [new AiEvidence("fact", "NAME", "AV Professionals", false, true)],
            ["Business: AV Professionals"]);

        await orchestrator.RunAsync(request, CancellationToken.None);
        await orchestrator.RunAsync(request, CancellationToken.None);

        Assert.Equal(1, inner.Calls);
    }

    private sealed class CountingOrchestrator : IAiOrchestrator
    {
        public int Calls { get; private set; }

        public Task<AiOrchestrationResult> RunAsync(AiOrchestrationRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new AiOrchestrationResult(
                "held",
                "Development",
                false,
                "gpt-4o-mini",
                "content.v1",
                "prompt",
                new AiValidationResult(true, true, false, false, AiConfidence.Low, AiRunStatus.Held, "held"),
                10,
                20,
                null));
        }
    }
}
