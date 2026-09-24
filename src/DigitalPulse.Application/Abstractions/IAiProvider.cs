using DigitalPulse.Domain.Ai;

namespace DigitalPulse.Application.Abstractions;

public sealed record AiCompletionRequest(
    string AgentCode,
    string Prompt,
    IReadOnlyList<AiEvidence> Evidence,
    IReadOnlyList<string> GraphContext);

public sealed record AiCompletionResponse(string Output, string ProviderName, bool IsLive);

public interface IAiProvider
{
    string ProviderName { get; }
    bool IsLive { get; }
    Task<AiCompletionResponse> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken);
}
