namespace DigitalPulse.Application.Abstractions;

public sealed record SearchDocument(
    Guid TenantId,
    Guid BusinessId,
    string Source,
    string Title,
    string Body,
    string Url);

public sealed record SearchHit(string Title, string Url, string Snippet, double Score);

public interface ISearchProvider
{
    string ProviderCode { get; }
    Task IndexAsync(SearchDocument document, CancellationToken cancellationToken);
    Task<IReadOnlyList<SearchHit>> SearchAsync(Guid tenantId, Guid businessId, string query, CancellationToken cancellationToken);
}

public interface IVectorSearchProvider
{
    string ProviderCode { get; }
    bool IsConfigured { get; }
    Task IndexAsync(SearchDocument document, CancellationToken cancellationToken);
    Task<IReadOnlyList<SearchHit>> SearchAsync(Guid tenantId, Guid businessId, string query, CancellationToken cancellationToken);
}
