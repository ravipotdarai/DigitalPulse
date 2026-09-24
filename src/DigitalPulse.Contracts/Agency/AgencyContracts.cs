namespace DigitalPulse.Contracts.Agency;

public sealed record CreateAgencyClientRequest(
    string Name,
    string? Website = null,
    string? Status = null,
    string? ContactName = null,
    string? ContactEmail = null,
    string? Notes = null,
    string? ExternalRef = null);

public sealed record UpdateAgencyClientRequest(
    string Status,
    string? ContactName = null,
    string? ContactEmail = null,
    string? Notes = null,
    string? ExternalRef = null);

public sealed record UpdateWhiteLabelRequest(
    string DisplayName,
    string? SupportEmail = null,
    string? SupportPhone = null,
    string? PrimaryColor = null,
    string? LogoUrl = null,
    string? CustomDomain = null,
    bool Enabled = false);

public sealed record StartAgencyWorkflowRequest(
    string Kind,
    Guid? ClientId = null);

public sealed record AdvanceAgencyWorkflowRequest(
    string? Note = null,
    bool Hold = false,
    string? HoldReason = null);

public sealed record AssembleAgencyReportRequest(
    string? Scope = null,
    Guid? ClientId = null);

public sealed record RecordAgencyDecisionRequest(string Decision);

public sealed record AgencyClientResponse(
    Guid Id,
    Guid BusinessId,
    Guid TenantId,
    string Name,
    string? Website,
    string Status,
    string? ContactName,
    string? ContactEmail,
    string? Notes,
    string? ExternalRef,
    int LocationCount,
    int OpenFindingCount,
    DateTimeOffset? LastScanAtUtc);

public sealed record WhiteLabelResponse(
    Guid Id,
    string DisplayName,
    string? SupportEmail,
    string? SupportPhone,
    string PrimaryColor,
    string? LogoUrl,
    string? CustomDomain,
    bool Enabled,
    bool Entitled,
    string HoldReason);

public sealed record AgencyWorkflowStepResponse(
    Guid Id,
    int Ordinal,
    string Name,
    bool Completed,
    string? Note);

public sealed record AgencyWorkflowResponse(
    Guid Id,
    Guid? ClientId,
    Guid? BusinessId,
    string Kind,
    string Status,
    int CurrentStep,
    string CurrentStepName,
    string HoldReason,
    IReadOnlyList<AgencyWorkflowStepResponse> Steps);

public sealed record AgencyReportLineResponse(
    Guid Id,
    Guid? BusinessId,
    string Kind,
    string Body);

public sealed record AgencyReportResponse(
    Guid Id,
    string Scope,
    Guid? ClientId,
    Guid? BusinessId,
    string Title,
    string ObservedFact,
    string Recommendation,
    string AiInterpretation,
    string? CustomerDecision,
    string HoldReason,
    IReadOnlyList<AgencyReportLineResponse> Lines);

public sealed record AgencyWorkspaceResponse(
    string TenantName,
    string TenantType,
    string PlanName,
    int ClientCap,
    bool WhiteLabelEntitled,
    string Note,
    WhiteLabelResponse WhiteLabel,
    IReadOnlyList<AgencyClientResponse> Clients,
    IReadOnlyList<AgencyWorkflowResponse> Workflows,
    IReadOnlyList<AgencyReportResponse> Reports);
