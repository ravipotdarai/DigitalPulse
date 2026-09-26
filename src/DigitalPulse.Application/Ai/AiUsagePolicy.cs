using System.Security.Cryptography;
using System.Text;
using DigitalPulse.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace DigitalPulse.Application.Ai;

public static class AiUsagePolicy
{
    public static int EstimateCostCents(int? promptTokens, int? completionTokens, int promptCentsPerThousand, int completionCentsPerThousand)
    {
        var prompt = Math.Max(0, promptTokens ?? 0);
        var completion = Math.Max(0, completionTokens ?? 0);
        return ((prompt * promptCentsPerThousand) + (completion * completionCentsPerThousand) + 999) / 1000;
    }

    public static string CacheKey(AiOrchestrationRequest request)
    {
        var raw = string.Join('\n',
            request.AgentCode,
            request.Ask.Trim(),
            request.Structured,
            string.Join('|', request.Evidence.Select(item => $"{item.Source}:{item.Title}:{item.Body}")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }
}

public sealed class CachingAiOrchestrator : IAiOrchestrator
{
    private readonly IAiOrchestrator _inner;
    private readonly IMemoryCache _cache;

    public CachingAiOrchestrator(IAiOrchestrator inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<AiOrchestrationResult> RunAsync(AiOrchestrationRequest request, CancellationToken cancellationToken)
    {
        var key = "ai:" + AiUsagePolicy.CacheKey(request);
        if (_cache.TryGetValue(key, out AiOrchestrationResult? cached) && cached is not null)
        {
            return cached;
        }

        var result = await _inner.RunAsync(request, cancellationToken);
        _cache.Set(key, result, TimeSpan.FromMinutes(15));
        return result;
    }
}
