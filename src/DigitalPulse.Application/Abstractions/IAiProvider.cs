using DigitalPulse.Domain.Ai;

namespace DigitalPulse.Application.Abstractions;

public sealed record AiCompletionRequest(
    string AgentCode,
    string Prompt,
    IReadOnlyList<AiEvidence> Evidence,
    IReadOnlyList<string> GraphContext,
    string? Model = null,
    int? MaxOutputTokens = null);

public sealed record AiCompletionResponse(
    string Output,
    string ProviderName,
    bool IsLive,
    string? Model = null,
    int? PromptTokens = null,
    int? CompletionTokens = null);

public interface IAiProvider
{
    string ProviderName { get; }
    bool IsLive { get; }
    Task<AiCompletionResponse> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken);
}

public interface IAiModelRouter
{
    string Select(string agentCode);
}
