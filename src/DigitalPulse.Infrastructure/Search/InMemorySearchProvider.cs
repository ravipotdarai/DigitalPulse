using System.Collections.Concurrent;
using DigitalPulse.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace DigitalPulse.Infrastructure.Search;

public sealed class InMemorySearchProvider : ISearchProvider
{
    private readonly ConcurrentDictionary<string, SearchDocument> _documents = new(StringComparer.OrdinalIgnoreCase);

    public string ProviderCode => "InMemory";

    public Task IndexAsync(SearchDocument document, CancellationToken cancellationToken)
    {
        if (document.TenantId == Guid.Empty || document.BusinessId == Guid.Empty)
        {
            throw new ArgumentException("Search documents must be tenant and business scoped.");
        }

        _documents[Key(document.TenantId, document.BusinessId, document.Url)] = document;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SearchHit>> SearchAsync(Guid tenantId, Guid businessId, string query, CancellationToken cancellationToken)
    {
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var hits = _documents.Values
            .Where(d => d.TenantId == tenantId && d.BusinessId == businessId)
            .Select(d => new SearchHit(d.Title, d.Url, Snippet(d.Body, query), Score(d, terms)))
            .Where(h => h.Score > 0)
            .OrderByDescending(h => h.Score)
            .Take(20)
            .ToList();
        return Task.FromResult<IReadOnlyList<SearchHit>>(hits);
    }

    private static string Key(Guid tenantId, Guid businessId, string url) => $"{tenantId:N}:{businessId:N}:{url}";

    private static double Score(SearchDocument document, IReadOnlyList<string> terms)
    {
        var haystack = $"{document.Title} {document.Body}";
        return terms.Count(term => haystack.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string Snippet(string body, string query)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        var index = body.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return body.Length <= 180 ? body : body[..180];
        }

        var start = Math.Max(0, index - 40);
        var length = Math.Min(180, body.Length - start);
        return body.Substring(start, length);
    }
}

public sealed class LocalHashVectorSearchProvider : IVectorSearchProvider
{
    private readonly ConcurrentDictionary<string, SearchDocument> _documents = new(StringComparer.OrdinalIgnoreCase);
    private readonly IConfiguration? _configuration;

    public LocalHashVectorSearchProvider(IConfiguration? configuration = null) => _configuration = configuration;

    public string ProviderCode => string.IsNullOrWhiteSpace(_configuration?["Ai:OpenAi:ApiKey"]) ? "LocalHash" : "OpenAI";
    public bool IsConfigured => true;

    public Task IndexAsync(SearchDocument document, CancellationToken cancellationToken)
    {
        _documents[$"{document.TenantId:N}:{document.BusinessId:N}:{document.Url}"] = document;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SearchHit>> SearchAsync(Guid tenantId, Guid businessId, string query, CancellationToken cancellationToken)
    {
        var needle = query.Trim();
        var hits = _documents.Values
            .Where(d => d.TenantId == tenantId && d.BusinessId == businessId)
            .Select(d =>
            {
                var haystack = $"{d.Title} {d.Body}";
                var score = needle.Length == 0 ? 0 : haystack.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Count(word => needle.Contains(word, StringComparison.OrdinalIgnoreCase) || word.Contains(needle, StringComparison.OrdinalIgnoreCase));
                return new SearchHit(d.Title, d.Url, haystack.Length <= 180 ? haystack : haystack[..180], score);
            })
            .Where(h => h.Score > 0)
            .OrderByDescending(h => h.Score)
            .Take(20)
            .ToList();
        return Task.FromResult<IReadOnlyList<SearchHit>>(hits);
    }
}

