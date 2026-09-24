namespace DigitalPulse.Contracts.WhatsApp;

public sealed record WhatsAppAccountResponse(
    Guid Id,
    string DisplayName,
    string PhoneNumber,
    string Status,
    string PhoneStatus,
    bool Connected,
    bool PhoneVerified,
    string HoldReason,
    string? WabaId,
    DateTimeOffset? LastHealthAtUtc,
    string? LastHealthDetail,
    bool CloudApiIsLive,
    string CloudApiName);

public sealed record WhatsAppContactResponse(
    Guid Id,
    string DisplayName,
    string Mobile,
    string Consent,
    DateTimeOffset? OptedInAtUtc,
    DateTimeOffset? OptedOutAtUtc,
    DateTimeOffset? LastInboundAtUtc,
    bool WindowOpen,
    Guid? CustomerId);

public sealed record WhatsAppTemplateResponse(
    Guid Id,
    string Name,
    string Language,
    string Category,
    string Body,
    string Status,
    string HoldReason);

public sealed record WhatsAppCampaignResponse(
    Guid Id,
    Guid TemplateId,
    string Name,
    string Status,
    DateTimeOffset? ScheduledAtUtc,
    string HoldReason,
    int AudienceCount,
    int SendCount,
    int HeldCount,
    int FailedCount);

public sealed record WhatsAppAttemptResponse(int Ordinal, string Outcome, string Detail, DateTimeOffset AtUtc);

public sealed record WhatsAppMessageResponse(
    Guid Id,
    Guid ContactId,
    Guid? ConversationId,
    Guid? CampaignId,
    Guid? TemplateId,
    string Kind,
    string Status,
    string Body,
    string HoldReason,
    bool Untrusted,
    string? ProviderMessageId,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<WhatsAppAttemptResponse> Attempts);

public sealed record WhatsAppConversationResponse(
    Guid Id,
    Guid ContactId,
    DateTimeOffset? LastInboundAtUtc,
    DateTimeOffset? LastOutboundAtUtc,
    DateTimeOffset? WindowOpenUntilUtc,
    bool WindowOpen,
    IReadOnlyList<WhatsAppMessageResponse> Messages);

public sealed record WhatsAppAnalyticsResponse(
    int OptedIn,
    int OptedOut,
    int TemplatesApproved,
    int Campaigns,
    int MessagesHeld,
    int MessagesFailed,
    int InboundUntrusted,
    string DeliveryNote);

public sealed record WhatsAppWorkspaceResponse(
    bool PlanEnabled,
    int MessagesPerMonth,
    int MessagesUsedThisMonth,
    string Note,
    WhatsAppAccountResponse? Account,
    WhatsAppAnalyticsResponse Analytics,
    IReadOnlyList<WhatsAppContactResponse> Contacts,
    IReadOnlyList<WhatsAppTemplateResponse> Templates,
    IReadOnlyList<WhatsAppCampaignResponse> Campaigns,
    IReadOnlyList<WhatsAppConversationResponse> Conversations,
    IReadOnlyList<WhatsAppMessageResponse> Messages);

public sealed record ConnectWhatsAppRequest(string DisplayName, string PhoneNumber);
public sealed record CreateWhatsAppTemplateRequest(string Name, string Language, string Category, string Body);
public sealed record CreateWhatsAppCampaignRequest(string Name, Guid TemplateId);
public sealed record ScheduleWhatsAppCampaignRequest(DateTimeOffset ScheduledAtUtc);
public sealed record DraftWhatsAppMessageRequest(Guid ContactId, string Kind, string Body, Guid? TemplateId);
public sealed record DraftWhatsAppAiRequest(Guid ContactId, string Kind, string Prompt, Guid? TemplateId);
public sealed record RecordWhatsAppInboundRequest(Guid ContactId, string Body);
public sealed record ReplyWhatsAppRequest(string Body);
