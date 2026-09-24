using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Ai;

public enum AiAgentKind
{
    Research = 0,
    BusinessIdentity = 1,
    Seo = 2,
    Content = 3,
    Social = 4,
    WhatsApp = 5,
    Reputation = 6,
    Portfolio = 7,
    Competitor = 8,
    Automation = 9
}

public enum AiRunStatus
{
    Held = 0,
    Completed = 1,
    NeedsReview = 2,
    Rejected = 3
}

public enum AiConfidence
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3
}

public enum KnowledgeKind
{
    Note = 0,
    Document = 1,
    Url = 2
}

public enum GraphNodeKind
{
    Business = 0,
    Service = 1,
    Brand = 2,
    Location = 3,
    Customer = 4,
    Contact = 5,
    Project = 6,
    Finding = 7,
    Platform = 8,
    ApprovedFact = 9,
    Permission = 10,
    Decision = 11,
    Knowledge = 12
}

public sealed record AiAgentDescriptor(AiAgentKind Kind, string Code, string Name, string Purpose);

public static class AiAgentCatalog
{
    public static readonly IReadOnlyList<AiAgentDescriptor> All =
    [
        new(AiAgentKind.Research, "research", "Research", "Assembles questions from the record. It does not invent market facts."),
        new(AiAgentKind.BusinessIdentity, "identity", "Business identity", "Restates the canonical identity. Restricted facts stay unpublished."),
        new(AiAgentKind.Seo, "seo", "SEO", "Compares stored website observations to the identity record."),
        new(AiAgentKind.Content, "content", "Content", "Drafts from approved projects and facts only."),
        new(AiAgentKind.Social, "social", "Social", "Channel copy from stored drafts and approved claims."),
        new(AiAgentKind.WhatsApp, "whatsapp", "WhatsApp messaging", "Template and session drafts. No unofficial send path."),
        new(AiAgentKind.Reputation, "reputation", "Reputation", "Reviews stored signals. It does not scrape live reviews."),
        new(AiAgentKind.Portfolio, "portfolio", "Portfolio", "Case-study outlines from recorded projects."),
        new(AiAgentKind.Competitor, "competitor", "Competitor", "Holds until a live research adapter exists."),
        new(AiAgentKind.Automation, "automation", "Automation", "Recommends assisted next steps. Autopilot waits for Phase 10.")
    ];

    public static AiAgentDescriptor Require(string code) =>
        All.FirstOrDefault(a => a.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException("Unknown agent.");
}

public sealed record AiEvidence(string Source, string Title, string Body, bool Restricted, bool Approved);

public sealed record AiValidationResult(
    bool Passed,
    bool HasEvidence,
    bool HasConflict,
    bool HasRestrictedFact,
    AiConfidence Confidence,
    AiRunStatus Status,
    string Summary);

public static class AiPolicy
{
    public static AiValidationResult Evaluate(IReadOnlyList<AiEvidence> evidence, string output, bool providerIsLive)
    {
        var hasEvidence = evidence.Count > 0;
        var hasRestricted = evidence.Any(e => e.Restricted);
        var approved = evidence.Where(e => e.Approved).ToList();
        var hasConflict = approved
            .GroupBy(e => e.Title, StringComparer.OrdinalIgnoreCase)
            .Any(g => g.Select(e => e.Body).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1);

        if (!hasEvidence)
        {
            return new AiValidationResult(
                false,
                false,
                false,
                false,
                AiConfidence.None,
                AiRunStatus.Rejected,
                "No evidence. DigitalPulse will not invent a factual claim.");
        }

        if (hasRestricted && MentionsRestricted(output, evidence))
        {
            return new AiValidationResult(
                false,
                true,
                hasConflict,
                true,
                AiConfidence.Low,
                AiRunStatus.NeedsReview,
                "Restricted facts appeared in the output. They must never be published.");
        }

        if (hasConflict)
        {
            return new AiValidationResult(
                false,
                true,
                true,
                hasRestricted,
                AiConfidence.Low,
                AiRunStatus.NeedsReview,
                "Conflicting approved evidence. A human must review before anything is used.");
        }

        if (!providerIsLive)
        {
            return new AiValidationResult(
                true,
                true,
                false,
                hasRestricted,
                AiConfidence.Low,
                AiRunStatus.Held,
                "Evidence was assembled. A live AI provider is not configured, so this stay is a development composition.");
        }

        var confidence = approved.Count >= 3 ? AiConfidence.High : AiConfidence.Medium;
        return new AiValidationResult(
            true,
            true,
            false,
            hasRestricted,
            confidence,
            AiRunStatus.Completed,
            "Validated against retrieved evidence. Low-risk autopilot is not enabled in this phase.");
    }

    private static bool MentionsRestricted(string output, IReadOnlyList<AiEvidence> evidence) =>
        evidence.Where(e => e.Restricted)
            .Any(e => !string.IsNullOrWhiteSpace(e.Body) &&
                      output.Contains(e.Body, StringComparison.OrdinalIgnoreCase));
}

public sealed class KnowledgeEntry : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public KnowledgeKind Kind { get; private set; }
    public string? SourceUrl { get; private set; }

    private KnowledgeEntry() { }

    public static KnowledgeEntry Create(Guid tenantId, Guid businessId, string title, string body, KnowledgeKind kind, string? sourceUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        return new KnowledgeEntry
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Title = title.Trim(),
            Body = body.Trim(),
            Kind = kind,
            SourceUrl = string.IsNullOrWhiteSpace(sourceUrl) ? null : sourceUrl.Trim()
        };
    }
}

public sealed class GraphNode : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public GraphNodeKind Kind { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string SourceKey { get; private set; } = string.Empty;
    public string? Value { get; private set; }

    private GraphNode() { }

    public static GraphNode Create(Guid tenantId, Guid businessId, GraphNodeKind kind, string label, string sourceKey, string? value)
    {
        return new GraphNode
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Kind = kind,
            Label = label.Trim(),
            SourceKey = sourceKey.Trim(),
            Value = value?.Trim()
        };
    }

    public void Replace(string label, string? value)
    {
        Label = label.Trim();
        Value = value?.Trim();
        Touch();
    }
}

public sealed class GraphEdge : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public Guid FromNodeId { get; private set; }
    public Guid ToNodeId { get; private set; }
    public string Relation { get; private set; } = string.Empty;

    private GraphEdge() { }

    public static GraphEdge Create(Guid tenantId, Guid businessId, Guid fromNodeId, Guid toNodeId, string relation) =>
        new()
        {
            TenantId = tenantId,
            BusinessId = businessId,
            FromNodeId = fromNodeId,
            ToNodeId = toNodeId,
            Relation = relation.Trim()
        };
}

public sealed class AiRun : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public AiAgentKind Agent { get; private set; }
    public string Prompt { get; private set; } = string.Empty;
    public AiRunStatus Status { get; private set; }
    public AiConfidence Confidence { get; private set; }
    public string Output { get; private set; } = string.Empty;
    public string ProviderName { get; private set; } = string.Empty;
    public bool ProviderIsLive { get; private set; }
    public string HoldReason { get; private set; } = string.Empty;

    private AiRun() { }

    public static AiRun Start(Guid tenantId, Guid businessId, AiAgentKind agent, string prompt) =>
        new()
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Agent = agent,
            Prompt = prompt.Trim(),
            Status = AiRunStatus.Held,
            Confidence = AiConfidence.None
        };

    public void Complete(AiValidationResult validation, string output, string providerName, bool providerIsLive, string? holdReason)
    {
        Status = validation.Status;
        Confidence = validation.Confidence;
        Output = output;
        ProviderName = providerName;
        ProviderIsLive = providerIsLive;
        HoldReason = holdReason ?? validation.Summary;
        Touch();
    }
}

public sealed class AiEvaluation : TenantOwnedEntity
{
    public Guid AiRunId { get; private set; }
    public bool Passed { get; private set; }
    public bool HasEvidence { get; private set; }
    public bool HasConflict { get; private set; }
    public bool HasRestrictedFact { get; private set; }
    public AiConfidence Confidence { get; private set; }
    public string Summary { get; private set; } = string.Empty;

    private AiEvaluation() { }

    public static AiEvaluation From(Guid tenantId, Guid runId, AiValidationResult result) =>
        new()
        {
            TenantId = tenantId,
            AiRunId = runId,
            Passed = result.Passed,
            HasEvidence = result.HasEvidence,
            HasConflict = result.HasConflict,
            HasRestrictedFact = result.HasRestrictedFact,
            Confidence = result.Confidence,
            Summary = result.Summary
        };
}

public sealed class AiAuditEvent : TenantOwnedEntity
{
    public Guid AiRunId { get; private set; }
    public string Stage { get; private set; } = string.Empty;
    public string Detail { get; private set; } = string.Empty;

    private AiAuditEvent() { }

    public static AiAuditEvent Record(Guid tenantId, Guid runId, string stage, string detail) =>
        new()
        {
            TenantId = tenantId,
            AiRunId = runId,
            Stage = stage,
            Detail = detail
        };
}
