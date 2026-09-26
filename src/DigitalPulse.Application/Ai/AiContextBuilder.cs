using DigitalPulse.Application.Abstractions;
using DigitalPulse.Domain.Ai;
using DigitalPulse.Domain.Businesses;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Ai;

public sealed record AiBuiltContext(
    IReadOnlyList<AiEvidence> Evidence,
    IReadOnlyList<string> GraphLines);

public interface IAiContextBuilder
{
    Task<AiBuiltContext> BuildAsync(Guid tenantId, Guid businessId, string ask, CancellationToken cancellationToken);
}

public sealed class AiContextBuilder : IAiContextBuilder
{
    private readonly IAppDbContext _db;
    private readonly ISearchProvider _search;

    public AiContextBuilder(IAppDbContext db, ISearchProvider search)
    {
        _db = db;
        _search = search;
    }

    public async Task<AiBuiltContext> BuildAsync(Guid tenantId, Guid businessId, string ask, CancellationToken cancellationToken)
    {
        var evidence = new List<AiEvidence>();
        var facts = await _db.Facts.AsNoTracking()
            .Where(f => f.BusinessId == businessId && f.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        foreach (var fact in facts)
        {
            evidence.Add(new AiEvidence(
                "fact",
                fact.FactTypeCode,
                fact.Value,
                fact.Status == FactStatus.Restricted,
                fact.Status == FactStatus.Approved));
        }

        var knowledge = await _db.KnowledgeEntries.AsNoTracking()
            .Where(k => k.BusinessId == businessId && k.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        foreach (var entry in knowledge)
        {
            evidence.Add(new AiEvidence("knowledge", entry.Title, entry.Body, false, true));
        }

        var nodes = await _db.GraphNodes.AsNoTracking()
            .Where(n => n.BusinessId == businessId && n.TenantId == tenantId &&
                        (n.Kind == GraphNodeKind.Business ||
                         n.Kind == GraphNodeKind.Service ||
                         n.Kind == GraphNodeKind.ApprovedFact ||
                         n.Kind == GraphNodeKind.Project ||
                         n.Kind == GraphNodeKind.Finding ||
                         n.Kind == GraphNodeKind.Location ||
                         n.Kind == GraphNodeKind.Content))
            .ToListAsync(cancellationToken);
        foreach (var node in nodes.Where(n => !string.IsNullOrWhiteSpace(n.Value)))
        {
            evidence.Add(new AiEvidence("graphify", node.Label, node.Value!, Restricted: false, Approved: true));
        }

        var hits = await _search.SearchAsync(tenantId, businessId, ask, cancellationToken);
        foreach (var hit in hits.Where(h => h.Score > 0).Take(8))
        {
            if (evidence.Any(e => e.Title.Equals(hit.Title, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            evidence.Add(new AiEvidence(_search.ProviderCode, hit.Title, hit.Snippet, false, true));
        }

        var graphLines = await _db.GraphNodes.AsNoTracking()
            .Where(n => n.BusinessId == businessId && n.TenantId == tenantId)
            .OrderBy(n => n.Kind)
            .Select(n => n.Value == null ? $"{n.Kind}: {n.Label}" : $"{n.Kind}: {n.Label} — {n.Value}")
            .ToListAsync(cancellationToken);

        return new AiBuiltContext(evidence, graphLines);
    }
}
