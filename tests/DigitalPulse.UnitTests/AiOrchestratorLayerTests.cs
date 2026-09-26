using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Ai;
using DigitalPulse.Domain.Ai;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class AiOrchestratorLayerTests
{
    [Fact]
    public async Task Orchestrator_uses_versioned_prompt_and_server_model()
    {
        var provider = new RecordingProvider();
        IAiOrchestrator orchestrator = new AiOrchestrator(provider, new FixedRouter());
        var evidence = new List<AiEvidence> { new("fact", "NAME", "AV Professionals", false, true) };

        var result = await orchestrator.RunAsync(
            new AiOrchestrationRequest("content", "Write a project outline", evidence, ["Business: AV Professionals"]),
            CancellationToken.None);

        Assert.Equal("content.v1", result.PromptVersion);
        Assert.Equal("server-standard", result.Model);
        Assert.Contains("Prompt-Version: content.v1", provider.LastPrompt);
        Assert.Contains("Draft from approved projects", provider.LastPrompt);
        Assert.Equal("server-standard", provider.LastModel);
        Assert.Equal(AiRunStatus.Held, result.Validation.Status);
    }

    [Fact]
    public async Task Structured_request_holds_when_output_is_not_json()
    {
        var provider = new LiveTextProvider();
        IAiOrchestrator orchestrator = new AiOrchestrator(provider, new FixedRouter());
        var evidence = new List<AiEvidence> { new("fact", "NAME", "AV Professionals", false, true) };

        var result = await orchestrator.RunAsync(
            new AiOrchestrationRequest("identity", "Return json about the business", evidence, ["Business: AV Professionals"], Structured: true),
            CancellationToken.None);

        Assert.Null(result.StructuredJson);
        Assert.Equal(AiRunStatus.NeedsReview, result.Validation.Status);
        Assert.Contains("JSON object", result.Validation.Summary);
    }

    private sealed class FixedRouter : IAiModelRouter
    {
        public string Select(string agentCode) => "server-standard";
    }

    private sealed class RecordingProvider : IAiProvider
    {
        public string? LastPrompt { get; private set; }
        public string? LastModel { get; private set; }
        public string ProviderName => "Development";
        public bool IsLive => false;

        public Task<AiCompletionResponse> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken)
        {
            LastPrompt = request.Prompt;
            LastModel = request.Model;
            return Task.FromResult(new AiCompletionResponse("Assembled from evidence.", ProviderName, false, request.Model));
        }
    }

    private sealed class LiveTextProvider : IAiProvider
    {
        public string ProviderName => "OpenAI";
        public bool IsLive => true;

        public Task<AiCompletionResponse> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new AiCompletionResponse("AV Professionals installs conference rooms.", ProviderName, true, request.Model));
    }
}
