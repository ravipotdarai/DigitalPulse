namespace DigitalPulse.Contracts.Ai;

public sealed record AiAgentResponse(string Code, string Name, string Purpose);

public sealed record KnowledgeEntryResponse(Guid Id, string Title, string Body, string Kind, string? SourceUrl, DateTimeOffset CreatedAtUtc);

public sealed record GraphNodeResponse(Guid Id, string Kind, string Label, string? Value);

public sealed record GraphEdgeResponse(Guid Id, Guid FromNodeId, Guid ToNodeId, string Relation);

public sealed record AiEvaluationResponse(
    bool Passed,
    bool HasEvidence,
    bool HasConflict,
    bool HasRestrictedFact,
    string Confidence,
    string Summary);

public sealed record AiAuditResponse(string Stage, string Detail, DateTimeOffset AtUtc);

public sealed record AiRunResponse(
    Guid Id,
    string Agent,
    string Prompt,
    string Status,
    string Confidence,
    string Output,
    string ProviderName,
    bool ProviderIsLive,
    string HoldReason,
    DateTimeOffset CreatedAtUtc,
    AiEvaluationResponse? Evaluation,
    IReadOnlyList<AiAuditResponse> Audit);

public sealed record AiWorkspaceResponse(
    string ProviderName,
    bool ProviderIsLive,
    string Note,
    IReadOnlyList<AiAgentResponse> Agents,
    IReadOnlyList<KnowledgeEntryResponse> Knowledge,
    IReadOnlyList<GraphNodeResponse> Nodes,
    IReadOnlyList<GraphEdgeResponse> Edges,
    IReadOnlyList<AiRunResponse> Runs);

public sealed record AddKnowledgeRequest(string Title, string Body, string Kind, string? SourceUrl);

public sealed record RunAiRequest(string Agent, string Prompt);
