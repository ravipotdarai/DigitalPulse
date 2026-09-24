using DigitalPulse.Application.Features.WhatsApp;
using DigitalPulse.Contracts.WhatsApp;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DigitalPulse.Api.Endpoints;

public static class WhatsAppEndpoints
{
    public static IEndpointRouteBuilder MapWhatsAppEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/businesses/{businessId:guid}/whatsapp").WithTags("WhatsApp").RequireAuthorization();
        group.MapGet("/", GetAsync);
        group.MapPost("/connect", ConnectAsync);
        group.MapPost("/phone/verify", VerifyPhoneAsync);
        group.MapPost("/contacts/import", ImportAsync);
        group.MapPost("/contacts/{contactId:guid}/opt-in", OptInAsync);
        group.MapPost("/contacts/{contactId:guid}/opt-out", OptOutAsync);
        group.MapPost("/templates", CreateTemplateAsync);
        group.MapPost("/templates/{templateId:guid}/approve", ApproveTemplateAsync);
        group.MapPost("/campaigns", CreateCampaignAsync);
        group.MapPost("/campaigns/{campaignId:guid}/approve", ApproveCampaignAsync);
        group.MapPost("/campaigns/{campaignId:guid}/schedule", ScheduleCampaignAsync);
        group.MapPost("/messages", DraftAsync);
        group.MapPost("/messages/draft-ai", DraftAiAsync);
        group.MapPost("/messages/{messageId:guid}/approve", ApproveMessageAsync);
        group.MapPost("/messages/{messageId:guid}/send", SendAsync);
        group.MapPost("/inbound", InboundAsync);
        group.MapPost("/conversations/{conversationId:guid}/reply", ReplyAsync);
        return app;
    }

    private static async Task<Ok<WhatsAppWorkspaceResponse>> GetAsync(
        Guid businessId, GetWhatsAppWorkspaceHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<WhatsAppWorkspaceResponse>> ConnectAsync(
        Guid businessId, ConnectWhatsAppRequest request, ConnectWhatsAppHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));

    private static async Task<Ok<WhatsAppWorkspaceResponse>> VerifyPhoneAsync(
        Guid businessId, VerifyWhatsAppPhoneHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<WhatsAppWorkspaceResponse>> ImportAsync(
        Guid businessId, ImportWhatsAppContactsHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<WhatsAppWorkspaceResponse>> OptInAsync(
        Guid businessId, Guid contactId, SetWhatsAppConsentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, contactId, true, cancellationToken));

    private static async Task<Ok<WhatsAppWorkspaceResponse>> OptOutAsync(
        Guid businessId, Guid contactId, SetWhatsAppConsentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, contactId, false, cancellationToken));

    private static async Task<Ok<WhatsAppTemplateResponse>> CreateTemplateAsync(
        Guid businessId, CreateWhatsAppTemplateRequest request, CreateWhatsAppTemplateHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));

    private static async Task<Ok<WhatsAppTemplateResponse>> ApproveTemplateAsync(
        Guid businessId, Guid templateId, ApproveWhatsAppTemplateHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, templateId, cancellationToken));

    private static async Task<Ok<WhatsAppCampaignResponse>> CreateCampaignAsync(
        Guid businessId, CreateWhatsAppCampaignRequest request, CreateWhatsAppCampaignHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));

    private static async Task<Ok<WhatsAppCampaignResponse>> ApproveCampaignAsync(
        Guid businessId, Guid campaignId, ApproveWhatsAppCampaignHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, campaignId, cancellationToken));

    private static async Task<Ok<WhatsAppCampaignResponse>> ScheduleCampaignAsync(
        Guid businessId, Guid campaignId, ScheduleWhatsAppCampaignRequest request, ScheduleWhatsAppCampaignHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, campaignId, request, cancellationToken));

    private static async Task<Ok<WhatsAppMessageResponse>> DraftAsync(
        Guid businessId, DraftWhatsAppMessageRequest request, DraftWhatsAppMessageHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));

    private static async Task<Ok<WhatsAppMessageResponse>> DraftAiAsync(
        Guid businessId, DraftWhatsAppAiRequest request, DraftWhatsAppAiHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));

    private static async Task<Ok<WhatsAppMessageResponse>> ApproveMessageAsync(
        Guid businessId, Guid messageId, ApproveWhatsAppMessageHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, messageId, cancellationToken));

    private static async Task<Ok<WhatsAppMessageResponse>> SendAsync(
        Guid businessId, Guid messageId, SendWhatsAppMessageHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, messageId, cancellationToken));

    private static async Task<Ok<WhatsAppWorkspaceResponse>> InboundAsync(
        Guid businessId, RecordWhatsAppInboundRequest request, RecordWhatsAppInboundHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));

    private static async Task<Ok<WhatsAppMessageResponse>> ReplyAsync(
        Guid businessId, Guid conversationId, ReplyWhatsAppRequest request, ReplyWhatsAppHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, conversationId, request, cancellationToken));
}
