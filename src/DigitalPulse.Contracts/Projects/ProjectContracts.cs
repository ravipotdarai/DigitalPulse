namespace DigitalPulse.Contracts.Projects;

public sealed record CreateProjectRequest(
    string Name,
    string? ClientName,
    string? Industry,
    string? Location,
    string? Description,
    string? Outcomes,
    DateOnly? StartedOn,
    DateOnly? CompletedOn,
    string PermissionScope,
    string Confidentiality);

public sealed record UpdateProjectRequest(
    string Name,
    string? ClientName,
    string? Industry,
    string? Location,
    string? Description,
    string? Outcomes,
    DateOnly? StartedOn,
    DateOnly? CompletedOn,
    string PermissionScope,
    string Confidentiality,
    string PublicationStatus);

public sealed record LinkNamedRequest(Guid Id);
public sealed record RegisterMediaRequest(string Label, string Kind, string? SourceUrl);
public sealed record DecideApprovalRequest(bool Approve, string Note);

public sealed record ProjectSummaryResponse(
    Guid Id,
    Guid BusinessId,
    string Name,
    string? ClientName,
    string PermissionScope,
    string Confidentiality,
    string PublicationStatus,
    DateTimeOffset UpdatedAtUtc);

public sealed record MediaAssetResponse(Guid Id, string Label, string Kind, string? SourceUrl, string Note);

public sealed record ContentVariantResponse(
    Guid Id,
    string Kind,
    string Title,
    string Body,
    string Status,
    string PublicationHold);

public sealed record ApprovalResponse(Guid Id, Guid ContentItemId, string Reason, bool Open, string? Decision, string? DecisionNote);

public sealed record ContentItemResponse(
    Guid Id,
    Guid ProjectId,
    string Title,
    string Status,
    string SourceNote,
    IReadOnlyList<ContentVariantResponse> Variants,
    IReadOnlyList<ApprovalResponse> Approvals);

public sealed record ProjectDetailResponse(
    ProjectSummaryResponse Project,
    string? Description,
    string? Outcomes,
    string? Industry,
    string? Location,
    DateOnly? StartedOn,
    DateOnly? CompletedOn,
    IReadOnlyList<string> Services,
    IReadOnlyList<string> Brands,
    IReadOnlyList<MediaAssetResponse> Media,
    IReadOnlyList<ContentItemResponse> Packs,
    string Note);

public sealed record ProjectWorkspaceResponse(
    IReadOnlyList<ProjectSummaryResponse> Projects,
    IReadOnlyList<NamedOptionResponse> Services,
    IReadOnlyList<NamedOptionResponse> Brands,
    string Note);

public sealed record NamedOptionResponse(Guid Id, string Name);
