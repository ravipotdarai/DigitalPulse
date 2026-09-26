using DigitalPulse.Application.Abstractions;
using DigitalPulse.Domain.Ai;
using DigitalPulse.Infrastructure.Ai;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class AiProviderAbstractionTests
{
    [Fact]
    public void Azure_and_openai_types_stay_in_infrastructure()
    {
        Assert.Equal("DigitalPulse.Infrastructure", typeof(AzureOpenAiProvider).Assembly.GetName().Name);
        Assert.Equal("DigitalPulse.Infrastructure", typeof(OpenAiProvider).Assembly.GetName().Name);
        Assert.Equal("DigitalPulse.Infrastructure", typeof(RetryingAiProvider).Assembly.GetName().Name);
    }

    [Fact]
    public async Task Retry_holds_after_failures_without_inventing_a_completion()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Ai:Retry:MaxAttempts"] = "3" })
            .Build();
        var inner = new FailingProvider();
        IAiProvider provider = new RetryingAiProvider(inner, config);

        var result = await provider.CompleteAsync(
            new AiCompletionRequest("content", "Ask: draft", [], []),
            CancellationToken.None);

        Assert.Equal(3, inner.Attempts);
        Assert.False(result.IsLive);
        Assert.Contains("did not invent", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Development_provider_still_assembles_evidence_only()
    {
        var provider = new DevelopmentAiProvider();
        var result = await provider.CompleteAsync(
            new AiCompletionRequest(
                "identity",
                "Ask: who are we",
                [new AiEvidence("fact", "NAME", "AV Professionals", false, true)],
                ["Business: AV Professionals"]),
            CancellationToken.None);

        Assert.False(result.IsLive);
        Assert.Contains("AV Professionals", result.Output);
        Assert.Contains("live AI provider is not configured", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FailingProvider : IAiProvider
    {
        public int Attempts { get; private set; }
        public string ProviderName => "OpenAI";
        public bool IsLive => true;

        public Task<AiCompletionResponse> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken)
        {
            Attempts++;
            return Task.FromResult(new AiCompletionResponse(
                "OpenAI returned 503. The run stays held. DigitalPulse did not invent a completion.",
                ProviderName,
                false,
                request.Model));
        }
    }
}
