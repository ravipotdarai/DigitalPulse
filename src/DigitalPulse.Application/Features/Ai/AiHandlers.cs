using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Identity;
using DigitalPulse.Contracts.Ai;
using DigitalPulse.Application.Features.Billing;
using DigitalPulse.Domain.Ai;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Projects;
using DigitalPulse.Domain.Scans;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Ai;

internal static class AiMaps
{
    public static KnowledgeKind ParseKind(string value) =>
        Enum.TryParse<KnowledgeKind>(value, true, out var parsed)
            ? parsed
            : throw AppException.Validation("Knowledge kind must be Note, Document, or Url.");

    public static KnowledgeEntryResponse ToResponse(this KnowledgeEntry entry) =>
        new(entry.Id, entry.Title, entry.Body, entry.Kind.ToString(), entry.SourceUrl, entry.CreatedAtUtc);

    public static GraphNodeResponse ToResponse(this GraphNode node) =>
        new(node.Id, node.Kind.ToString(), node.Label, node.Value);

    public static GraphEdgeResponse ToResponse(this GraphEdge edge) =>
        new(edge.Id, edge.FromNodeId, edge.ToNodeId, edge.Relation);

    public static AiAgentResponse ToResponse(this AiAgentDescriptor agent) =>
        new(agent.Code, agent.Name, agent.Purpose);

    public static AiRunResponse ToResponse(
        this AiRun run,
        AiEvaluation? evaluation,
        IReadOnlyList<AiAuditEvent> audit) =>
        new(
            run.Id,
            AiAgentCatalog.All.First(a => a.Kind == run.Agent).Code,
            run.Prompt,
            run.Status.ToString(),
            run.Confidence.ToString(),
            run.Output,
            run.ProviderName,
            run.ProviderIsLive,
            run.HoldReason,
            run.CreatedAtUtc,
            evaluation is null
                ? null
                : new AiEvaluationResponse(
                    evaluation.Passed,
                    evaluation.HasEvidence,
                    evaluation.HasConflict,
                    evaluation.HasRestrictedFact,
                    evaluation.Confidence.ToString(),
                    evaluation.Summary),
            audit.Select(a => new AiAuditResponse(a.Stage, a.Detail, a.CreatedAtUtc)).ToList());
}

public sealed class GetAiWorkspaceHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAiProvider _provider;
    private readonly ISearchProvider _search;

    public GetAiWorkspaceHandler(IAppDbContext db, ITenantContext tenant, IAiProvider provider, ISearchProvider search)
    {
        _db = db;
        _tenant = tenant;
        _provider = provider;
        _search = search;
    }

    public async Task<AiWorkspaceResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        if (!await _db.GraphNodes.AnyAsync(n => n.BusinessId == businessId, cancellationToken))
        {
            await GraphifySync.RebuildAsync(_db, tenantId, businessId, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        await KnowledgeIndex.ReindexAsync(_db, _search, tenantId, businessId, cancellationToken);
        return await AiWorkspaceLoader.LoadAsync(_db, _provider, businessId, cancellationToken);
    }
}

public sealed class AddKnowledgeHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ISearchProvider _search;

    public AddKnowledgeHandler(IAppDbContext db, ITenantContext tenant, ISearchProvider search)
    {
        _db = db;
        _tenant = tenant;
        _search = search;
    }

    public async Task<KnowledgeEntryResponse> Handle(Guid businessId, AddKnowledgeRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var entry = KnowledgeEntry.Create(
            tenantId,
            businessId,
            request.Title,
            request.Body,
            AiMaps.ParseKind(request.Kind),
            request.SourceUrl);
        _db.KnowledgeEntries.Add(entry);
        await KnowledgeIndex.IndexAsync(_search, entry, cancellationToken);
        await GraphifySync.AttachKnowledgeAsync(_db, tenantId, businessId, entry, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return entry.ToResponse();
    }
}

public sealed class SyncGraphHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAiProvider _provider;

    public SyncGraphHandler(IAppDbContext db, ITenantContext tenant, IAiProvider provider)
    {
        _db = db;
        _tenant = tenant;
        _provider = provider;
    }

    public async Task<AiWorkspaceResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        await GraphifySync.RebuildAsync(_db, tenantId, businessId, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await AiWorkspaceLoader.LoadAsync(_db, _provider, businessId, cancellationToken);
    }
}

public sealed class RunAiHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAiProvider _provider;
    private readonly ISearchProvider _search;

    public RunAiHandler(IAppDbContext db, ITenantContext tenant, IAiProvider provider, ISearchProvider search)
    {
        _db = db;
        _tenant = tenant;
        _provider = provider;
        _search = search;
    }

    public async Task<AiRunResponse> Handle(Guid businessId, RunAiRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial), cancellationToken)
            ?? throw AppException.Validation("Complete plan selection before running the orchestrator.");
        var plan = await _db.Plans.AsNoTracking().FirstAsync(p => p.Id == subscription.PlanId, cancellationToken);
        var start = BillingPolicy.PeriodStart();
        var used = await _db.AiRuns.CountAsync(r => r.TenantId == tenantId && r.CreatedAtUtc >= start, cancellationToken);
        try
        {
            EntitlementRules.EnsureCanRunAi(plan, used);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        if (string.IsNullOrWhiteSpace(request.Prompt) || request.Prompt.Trim().Length < 4)
        {
            throw AppException.Validation("Ask at least four characters so retrieval has something to match.");
        }

        AiAgentDescriptor agent;
        try
        {
            agent = AiAgentCatalog.Require(request.Agent);
        }
        catch (ArgumentException)
        {
            throw AppException.Validation("Unknown agent.");
        }

        if (!await _db.GraphNodes.AnyAsync(n => n.BusinessId == businessId, cancellationToken))
        {
            await GraphifySync.RebuildAsync(_db, tenantId, businessId, cancellationToken);
        }

        var run = AiRun.Start(tenantId, businessId, agent.Kind, request.Prompt);
        _db.AiRuns.Add(run);
        Audit(tenantId, run.Id, "policy", "Session tenant authorized. Client tenant headers are not trusted.");

        var evidence = await RetrieveEvidenceAsync(tenantId, businessId, request.Prompt.Trim(), cancellationToken);
        var graphLines = await _db.GraphNodes.AsNoTracking()
            .Where(n => n.BusinessId == businessId)
            .OrderBy(n => n.Kind)
            .Select(n => $"{n.Kind}: {n.Label}" + (n.Value == null ? "" : $" — {n.Value}"))
            .ToListAsync(cancellationToken);

        Audit(tenantId, run.Id, "graphify", graphLines.Count == 0
            ? "Graphify had no nodes for this business."
            : $"Retrieved {graphLines.Count} Graphify nodes.");
        Audit(tenantId, run.Id, "retrieval", evidence.Count == 0
            ? "Knowledge retrieval returned no evidence."
            : $"Retrieved {evidence.Count} evidence items from Graphify, facts, and the knowledge base.");

        var prompt = BuildPrompt(agent, request.Prompt.Trim(), evidence, graphLines);
        Audit(tenantId, run.Id, "prompt", $"Constructed {agent.Name} prompt from retrieved context only.");

        var completion = await _provider.CompleteAsync(
            new AiCompletionRequest(agent.Code, prompt, evidence, graphLines),
            cancellationToken);
        Audit(tenantId, run.Id, "provider", completion.IsLive
            ? $"{completion.ProviderName} returned a live completion."
            : $"{completion.ProviderName} composed a development brief. No live model was called.");

        var validation = AiPolicy.Evaluate(evidence, completion.Output, completion.IsLive);
        Audit(tenantId, run.Id, "validation", validation.Summary);
        Audit(tenantId, run.Id, "policy", validation.HasRestrictedFact
            ? "Restricted facts must never be published."
            : "Business policy: low confidence stays assisted. Autopilot is Phase 10.");

        var hold = validation.Status switch
        {
            AiRunStatus.Rejected => validation.Summary,
            AiRunStatus.NeedsReview => validation.Summary,
            AiRunStatus.Held => validation.Summary,
            _ => completion.IsLive
                ? "Validated. Execution stays assisted until Phase 10."
                : validation.Summary
        };

        run.Complete(validation, Clip(completion.Output, 4000), completion.ProviderName, completion.IsLive, Clip(hold, 500));
        _db.AiEvaluations.Add(AiEvaluation.From(tenantId, run.Id, validation));
        Audit(tenantId, run.Id, "approval",
            validation.Status is AiRunStatus.Completed
                ? "Eligible for assisted use only. No action was executed."
                : "Held for review. Nothing was published or executed.");

        await GraphifySync.RecordDecisionAsync(_db, tenantId, businessId, run, validation, cancellationToken);
        Audit(tenantId, run.Id, "graphify", "Graphify recorded the decision and outcome.");
        Audit(tenantId, run.Id, "audit", "Run stored with evaluation and stage history.");
        await UsageMeter.RecordAsync(_db, tenantId, UsageKind.AiGeneration, agent.Code, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        var evaluation = await _db.AiEvaluations.AsNoTracking().FirstAsync(e => e.AiRunId == run.Id, cancellationToken);
        var audit = await _db.AiAuditEvents.AsNoTracking()
            .Where(a => a.AiRunId == run.Id)
            .OrderBy(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return run.ToResponse(evaluation, audit);
    }

    private async Task<List<AiEvidence>> RetrieveEvidenceAsync(
        Guid tenantId,
        Guid businessId,
        string prompt,
        CancellationToken cancellationToken)
    {
        var evidence = new List<AiEvidence>();
        var facts = await _db.Facts.AsNoTracking()
            .Where(f => f.BusinessId == businessId)
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
            .Where(k => k.BusinessId == businessId)
            .ToListAsync(cancellationToken);
        foreach (var entry in knowledge)
        {
            evidence.Add(new AiEvidence("knowledge", entry.Title, entry.Body, false, true));
        }

        var nodes = await _db.GraphNodes.AsNoTracking()
            .Where(n => n.BusinessId == businessId &&
                        (n.Kind == GraphNodeKind.Business ||
                         n.Kind == GraphNodeKind.Service ||
                         n.Kind == GraphNodeKind.ApprovedFact ||
                         n.Kind == GraphNodeKind.Project ||
                         n.Kind == GraphNodeKind.Finding))
            .ToListAsync(cancellationToken);
        foreach (var node in nodes.Where(n => !string.IsNullOrWhiteSpace(n.Value)))
        {
            evidence.Add(new AiEvidence("graphify", node.Label, node.Value!, Restricted: false, Approved: true));
        }

        var hits = await _search.SearchAsync(tenantId, businessId, prompt, cancellationToken);
        foreach (var hit in hits.Where(h => h.Score > 0).Take(8))
        {
            if (evidence.Any(e => e.Title.Equals(hit.Title, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            evidence.Add(new AiEvidence(_search.ProviderCode, hit.Title, hit.Snippet, false, true));
        }

        return evidence;
    }

    private static string BuildPrompt(
        AiAgentDescriptor agent,
        string ask,
        IReadOnlyList<AiEvidence> evidence,
        IReadOnlyList<string> graph)
    {
        var lines = new List<string>
        {
            $"Agent: {agent.Name}. {agent.Purpose}",
            $"Ask: {ask}",
            "Rules: no evidence → no factual claim. Restricted facts must never be published. Conflicting evidence requires review.",
            "Graphify:"
        };
        lines.AddRange(graph.Take(40).Select(g => $"- {g}"));
        lines.Add("Evidence:");
        lines.AddRange(evidence.Select(e =>
            $"- [{e.Source}] {e.Title}: {(e.Restricted ? "(restricted, do not publish)" : e.Body)}"));
        return string.Join(Environment.NewLine, lines);
    }

    private void Audit(Guid tenantId, Guid runId, string stage, string detail) =>
        _db.AiAuditEvents.Add(AiAuditEvent.Record(tenantId, runId, stage, Clip(detail, 500)));

    private static string Clip(string value, int max) =>
        string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}

internal static class AiWorkspaceLoader
{
    public static async Task<AiWorkspaceResponse> LoadAsync(
        IAppDbContext db,
        IAiProvider provider,
        Guid businessId,
        CancellationToken cancellationToken)
    {
        var knowledge = await db.KnowledgeEntries.AsNoTracking()
            .Where(k => k.BusinessId == businessId)
            .OrderByDescending(k => k.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var nodes = await db.GraphNodes.AsNoTracking()
            .Where(n => n.BusinessId == businessId)
            .OrderBy(n => n.Kind).ThenBy(n => n.Label)
            .ToListAsync(cancellationToken);
        var edges = await db.GraphEdges.AsNoTracking()
            .Where(e => e.BusinessId == businessId)
            .ToListAsync(cancellationToken);
        var runs = await db.AiRuns.AsNoTracking()
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);
        var runIds = runs.Select(r => r.Id).ToList();
        var evaluations = await db.AiEvaluations.AsNoTracking()
            .Where(e => runIds.Contains(e.AiRunId))
            .ToListAsync(cancellationToken);
        var audit = await db.AiAuditEvents.AsNoTracking()
            .Where(a => runIds.Contains(a.AiRunId))
            .OrderBy(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return new AiWorkspaceResponse(
            provider.ProviderName,
            provider.IsLive,
            provider.IsLive
                ? "Live completions still pass validation. Restricted facts never publish. Autopilot waits for Phase 10."
                : "A live AI provider is not configured. Runs stay Held with an evidence-only composition. Nothing is invented as a model answer.",
            AiAgentCatalog.All.Select(a => a.ToResponse()).ToList(),
            knowledge.Select(k => k.ToResponse()).ToList(),
            nodes.Select(n => n.ToResponse()).ToList(),
            edges.Select(e => e.ToResponse()).ToList(),
            runs.Select(r => r.ToResponse(
                evaluations.FirstOrDefault(e => e.AiRunId == r.Id),
                audit.Where(a => a.AiRunId == r.Id).ToList())).ToList());
    }
}

internal static class KnowledgeIndex
{
    public static Task IndexAsync(ISearchProvider search, KnowledgeEntry entry, CancellationToken cancellationToken) =>
        search.IndexAsync(
            new SearchDocument(
                entry.TenantId,
                entry.BusinessId,
                "knowledge",
                entry.Title,
                entry.Body,
                entry.SourceUrl ?? $"knowledge://{entry.Id:N}"),
            cancellationToken);

    public static async Task ReindexAsync(
        IAppDbContext db,
        ISearchProvider search,
        Guid tenantId,
        Guid businessId,
        CancellationToken cancellationToken)
    {
        var entries = await db.KnowledgeEntries.AsNoTracking()
            .Where(k => k.BusinessId == businessId && k.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        foreach (var entry in entries)
        {
            await IndexAsync(search, entry, cancellationToken);
        }
    }
}

internal static class GraphifySync
{
    public static async Task RebuildAsync(IAppDbContext db, Guid tenantId, Guid businessId, CancellationToken cancellationToken)
    {
        var staleNodes = await db.GraphNodes.Where(n => n.BusinessId == businessId).ToListAsync(cancellationToken);
        var staleEdges = await db.GraphEdges.Where(e => e.BusinessId == businessId).ToListAsync(cancellationToken);
        db.GraphEdges.RemoveRange(staleEdges);
        db.GraphNodes.RemoveRange(staleNodes);

        var business = await db.Businesses.FirstAsync(b => b.Id == businessId, cancellationToken);
        var businessNode = Add(db, tenantId, businessId, GraphNodeKind.Business, business.Name, $"business:{business.Id}",
            string.Join(" · ", new[] { business.Website, business.IndustryCode, business.BrandVoice }.Where(v => !string.IsNullOrWhiteSpace(v))));

        foreach (var service in await db.Services.Where(s => s.BusinessId == businessId).ToListAsync(cancellationToken))
        {
            Link(db, tenantId, businessId, businessNode, Add(db, tenantId, businessId, GraphNodeKind.Service, service.Name, $"service:{service.Id}", service.Description), "offers");
        }

        var brandIds = await db.BusinessBrands.Where(b => b.BusinessId == businessId).Select(b => b.BrandId).ToListAsync(cancellationToken);
        foreach (var brand in await db.Brands.Where(b => brandIds.Contains(b.Id)).ToListAsync(cancellationToken))
        {
            Link(db, tenantId, businessId, businessNode, Add(db, tenantId, businessId, GraphNodeKind.Brand, brand.Name, $"brand:{brand.Id}", null), "brands");
        }

        foreach (var location in await db.Locations.Where(l => l.BusinessId == businessId).ToListAsync(cancellationToken))
        {
            var value = string.Join(", ", new[] { location.AddressLine, location.City, location.Region, location.CountryCode }.Where(v => !string.IsNullOrWhiteSpace(v)));
            Link(db, tenantId, businessId, businessNode, Add(db, tenantId, businessId, GraphNodeKind.Location, location.Name, $"location:{location.Id}", value), "located-at");
        }

        foreach (var customer in await db.Customers.Where(c => c.BusinessId == businessId).ToListAsync(cancellationToken))
        {
            var customerNode = Add(db, tenantId, businessId, GraphNodeKind.Customer, customer.DisplayName, $"customer:{customer.Id}", customer.Notes);
            Link(db, tenantId, businessId, businessNode, customerNode, "serves");
            foreach (var contact in await db.CustomerContacts.Where(c => c.CustomerId == customer.Id).ToListAsync(cancellationToken))
            {
                Link(db, tenantId, businessId, customerNode, Add(db, tenantId, businessId, GraphNodeKind.Contact, $"{contact.Kind} {contact.Value}", $"contact:{contact.Id}", contact.Value), "contact");
            }
        }

        foreach (var project in await db.Projects.Where(p => p.BusinessId == businessId).ToListAsync(cancellationToken))
        {
            var projectNode = Add(db, tenantId, businessId, GraphNodeKind.Project, project.Name, $"project:{project.Id}", project.Description);
            Link(db, tenantId, businessId, businessNode, projectNode, "delivered");
            Link(db, tenantId, businessId, projectNode,
                Add(db, tenantId, businessId, GraphNodeKind.Permission, project.PermissionScope.ToString(), $"permission:{project.Id}", project.Confidentiality.ToString()),
                "permission");
        }

        foreach (var finding in await db.Findings.Where(f => f.BusinessId == businessId && f.Status == FindingStatus.Open).ToListAsync(cancellationToken))
        {
            Link(db, tenantId, businessId, businessNode,
                Add(db, tenantId, businessId, GraphNodeKind.Finding, finding.Title, $"finding:{finding.Id}", finding.Description),
                "signal");
        }

        foreach (var connection in await db.Connections.Where(c => c.BusinessId == businessId).ToListAsync(cancellationToken))
        {
            Link(db, tenantId, businessId, businessNode,
                Add(db, tenantId, businessId, GraphNodeKind.Platform, connection.PlatformCode, $"platform:{connection.Id}", connection.Status.ToString()),
                "connected-to");
        }

        foreach (var fact in await db.Facts.Where(f => f.BusinessId == businessId && f.Status == FactStatus.Approved).ToListAsync(cancellationToken))
        {
            Link(db, tenantId, businessId, businessNode,
                Add(db, tenantId, businessId, GraphNodeKind.ApprovedFact, fact.FactTypeCode, $"fact:{fact.Id}", fact.Value),
                "claims");
        }

        foreach (var entry in await db.KnowledgeEntries.Where(k => k.BusinessId == businessId).ToListAsync(cancellationToken))
        {
            Link(db, tenantId, businessId, businessNode,
                Add(db, tenantId, businessId, GraphNodeKind.Knowledge, entry.Title, $"knowledge:{entry.Id}", entry.Body),
                "knows");
        }
    }

    public static async Task AttachKnowledgeAsync(
        IAppDbContext db,
        Guid tenantId,
        Guid businessId,
        KnowledgeEntry entry,
        CancellationToken cancellationToken)
    {
        var businessNode = await db.GraphNodes.FirstOrDefaultAsync(
            n => n.BusinessId == businessId && n.Kind == GraphNodeKind.Business, cancellationToken);
        if (businessNode is null)
        {
            await RebuildAsync(db, tenantId, businessId, cancellationToken);
            return;
        }

        var node = Add(db, tenantId, businessId, GraphNodeKind.Knowledge, entry.Title, $"knowledge:{entry.Id}", entry.Body);
        Link(db, tenantId, businessId, businessNode, node, "knows");
    }

    public static async Task RecordDecisionAsync(
        IAppDbContext db,
        Guid tenantId,
        Guid businessId,
        AiRun run,
        AiValidationResult validation,
        CancellationToken cancellationToken)
    {
        var businessNode = await db.GraphNodes.FirstOrDefaultAsync(
            n => n.BusinessId == businessId && n.Kind == GraphNodeKind.Business, cancellationToken);
        if (businessNode is null)
        {
            return;
        }

        var decision = Add(
            db,
            tenantId,
            businessId,
            GraphNodeKind.Decision,
            $"{run.Agent} {validation.Status}",
            $"decision:{run.Id}",
            validation.Summary);
        Link(db, tenantId, businessId, businessNode, decision, "decided");
    }

    private static GraphNode Add(
        IAppDbContext db,
        Guid tenantId,
        Guid businessId,
        GraphNodeKind kind,
        string label,
        string sourceKey,
        string? value)
    {
        var node = GraphNode.Create(tenantId, businessId, kind, label, sourceKey, Clip(string.IsNullOrWhiteSpace(value) ? null : value, 2000));
        db.GraphNodes.Add(node);
        return node;
    }

    private static void Link(IAppDbContext db, Guid tenantId, Guid businessId, GraphNode from, GraphNode to, string relation) =>
        db.GraphEdges.Add(GraphEdge.Create(tenantId, businessId, from.Id, to.Id, relation));

    private static string? Clip(string? value, int max) =>
        value is null || value.Length <= max ? value : value[..max];
}
