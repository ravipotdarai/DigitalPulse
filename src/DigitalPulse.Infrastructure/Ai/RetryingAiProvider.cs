using DigitalPulse.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace DigitalPulse.Infrastructure.Ai;

public sealed class RetryingAiProvider : IAiProvider
{
    private readonly IAiProvider _inner;
    private readonly int _attempts;

    public RetryingAiProvider(IAiProvider inner, IConfiguration configuration)
    {
        _inner = inner;
        _attempts = Math.Clamp(configuration.GetValue("Ai:Retry:MaxAttempts", 3), 1, 5);
    }

    public string ProviderName => _inner.ProviderName;
    public bool IsLive => _inner.IsLive;

    public async Task<AiCompletionResponse> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken)
    {
        AiCompletionResponse? last = null;
        for (var attempt = 1; attempt <= _attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                last = await _inner.CompleteAsync(request, cancellationToken);
                if (last.IsLive || attempt == _attempts)
                {
                    return last;
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                last = new AiCompletionResponse(
                    $"{_inner.ProviderName} timed out. The run stays held. DigitalPulse did not invent a completion.",
                    _inner.ProviderName,
                    false,
                    request.Model);
                if (attempt == _attempts) return last;
            }
            catch (HttpRequestException)
            {
                last = new AiCompletionResponse(
                    $"{_inner.ProviderName} was unreachable. The run stays held. DigitalPulse did not invent a completion.",
                    _inner.ProviderName,
                    false,
                    request.Model);
                if (attempt == _attempts) return last;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
        }

        return last ?? new AiCompletionResponse(
            $"{_inner.ProviderName} did not return a completion. The run stays held.",
            _inner.ProviderName,
            false,
            request.Model);
    }
}
