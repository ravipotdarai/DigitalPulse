using DigitalPulse.Application.Features.Content;
using DigitalPulse.Contracts.Content;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DigitalPulse.Api.Endpoints;

public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/businesses/{businessId:guid}/content").WithTags("Content").RequireAuthorization();
        group.MapGet("/", GetWorkspaceAsync);
        group.MapGet("/{contentId:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{contentId:guid}", UpdateAsync);
        group.MapDelete("/{contentId:guid}", DeleteAsync);
        group.MapPost("/{contentId:guid}/approve", ApproveAsync);
        group.MapPost("/{contentId:guid}/reject", RejectAsync);
        group.MapPost("/{contentId:guid}/schedule", ScheduleAsync);
        group.MapPost("/{contentId:guid}/schedule/cancel", CancelScheduleAsync);
        group.MapPost("/{contentId:guid}/publish", PublishAsync);
        group.MapPost("/{contentId:guid}/archive", ArchiveAsync);
        group.MapPost("/{contentId:guid}/revisions/{revisionId:guid}/restore", RestoreAsync);
        group.MapPost("/{contentId:guid}/media", AttachMediaAsync);
        group.MapPost("/media", RegisterMediaAsync);
        group.MapPost("/media/upload", UploadMediaAsync).DisableAntiforgery();
        group.MapPost("/calendar/release", ReleaseAsync);
        group.MapPost("/{contentId:guid}/seo", AnalyzeAsync);
        group.MapPost("/{contentId:guid}/variants", VariantsAsync);
        group.MapPost("/{contentId:guid}/distribute", DistributeAsync);
        group.MapPost("/opportunities/discover", DiscoverAsync);
        group.MapPost("/opportunities", CreateOpportunityAsync);
        group.MapPost("/opportunities/{opportunityId:guid}/dismiss", DismissOpportunityAsync);
        group.MapPost("/generate", GenerateAsync);
        group.MapPost("/assist", AssistAsync);

        app.MapGet("/v1/hub/{businessId:guid}", GetPublicIndexAsync).WithTags("Content").AllowAnonymous();
        app.MapGet("/v1/hub/{businessId:guid}/media/{assetId:guid}", GetPublicMediaAsync).WithTags("Content").AllowAnonymous();
        app.MapGet("/v1/hub/{businessId:guid}/{slug}", GetPublicAsync).WithTags("Content").AllowAnonymous();
        return app;
    }

    private static async Task<Ok<ContentHubWorkspace>> GetWorkspaceAsync(
        Guid businessId, GetContentHubHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<HubContentResponse>> GetAsync(
        Guid businessId, Guid contentId, GetHubContentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, contentId, cancellationToken));

    private static async Task<Ok<HubContentResponse>> CreateAsync(
        Guid businessId, CreateHubContentRequest request, CreateHubContentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));

    private static async Task<Ok<HubContentResponse>> UpdateAsync(
        Guid businessId, Guid contentId, UpdateHubContentRequest request, UpdateHubContentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, contentId, request, cancellationToken));

    private static async Task<NoContent> DeleteAsync(
        Guid businessId, Guid contentId, DeleteHubContentHandler handler, CancellationToken cancellationToken)
    {
        await handler.Handle(businessId, contentId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<HubContentResponse>> ApproveAsync(
        Guid businessId, Guid contentId, ApproveHubContentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, contentId, cancellationToken));

    private static async Task<Ok<HubContentResponse>> RejectAsync(
        Guid businessId, Guid contentId, RejectHubContentRequest request, RejectHubContentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, contentId, request, cancellationToken));

    private static async Task<Ok<HubContentResponse>> CancelScheduleAsync(
        Guid businessId, Guid contentId, CancelHubScheduleHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, contentId, cancellationToken));

    private static async Task<Ok<HubContentResponse>> RestoreAsync(
        Guid businessId, Guid contentId, Guid revisionId, RestoreHubRevisionHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, contentId, revisionId, cancellationToken));

    private static async Task<Ok<HubContentResponse>> AttachMediaAsync(
        Guid businessId, Guid contentId, AttachHubMediaRequest request, AttachHubMediaHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, contentId, request, cancellationToken));

    private static async Task<Ok<HubMediaAssetResponse>> RegisterMediaAsync(
        Guid businessId, RegisterHubMediaRequest request, RegisterHubMediaHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));

    private static async Task<Ok<HubMediaAssetResponse>> UploadMediaAsync(
        Guid businessId,
        IFormFile file,
        UploadHubMediaHandler handler,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return TypedResults.Ok(await handler.Handle(
            businessId,
            file.FileName,
            file.ContentType ?? string.Empty,
            file.Length,
            stream,
            cancellationToken));
    }

    private static async Task<IResult> GetPublicMediaAsync(
        Guid businessId, Guid assetId, GetHubMediaFileHandler handler, CancellationToken cancellationToken)
    {
        var file = await handler.Handle(businessId, assetId, cancellationToken);
        return Results.File(file.Bytes, file.ContentType, file.FileName);
    }

    private static async Task<Ok<HubContentResponse>> ScheduleAsync(
        Guid businessId, Guid contentId, ScheduleHubContentRequest request, ScheduleHubContentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, contentId, request, cancellationToken));

    private static async Task<Ok<HubContentResponse>> PublishAsync(
        Guid businessId, Guid contentId, PublishHubContentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, contentId, cancellationToken));

    private static async Task<Ok<HubContentResponse>> ArchiveAsync(
        Guid businessId, Guid contentId, ArchiveHubContentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, contentId, cancellationToken));

    private static async Task<Ok<ContentHubWorkspace>> ReleaseAsync(
        Guid businessId, ReleaseScheduledHubContentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<HubContentResponse>> AnalyzeAsync(
        Guid businessId, Guid contentId, AnalyzeHubSeoRequest? request, AnalyzeHubSeoHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, contentId, request?.FocusKeyword, cancellationToken));

    private static async Task<Ok<HubContentResponse>> VariantsAsync(
        Guid businessId, Guid contentId, CreateHubVariantsHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, contentId, cancellationToken));

    private static async Task<Ok<HubContentResponse>> DistributeAsync(
        Guid businessId, Guid contentId, DistributeHubContentRequest request, DistributeHubContentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, contentId, request.ProviderCode, cancellationToken));

    private static async Task<Ok<ContentHubWorkspace>> DiscoverAsync(
        Guid businessId, DiscoverContentOpportunitiesHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<ContentHubWorkspace>> CreateOpportunityAsync(
        Guid businessId, CreateContentOpportunityRequest request, CreateContentOpportunityHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));

    private static async Task<Ok<ContentHubWorkspace>> DismissOpportunityAsync(
        Guid businessId, Guid opportunityId, DismissContentOpportunityHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, opportunityId, cancellationToken));

    private static async Task<Ok<HubContentResponse>> GenerateAsync(
        Guid businessId, GenerateHubContentRequest request, GenerateHubContentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));

    private static async Task<Ok<AssistHubContentResponse>> AssistAsync(
        Guid businessId, AssistHubContentRequest request, AssistHubContentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));

    private static async Task<Ok<PublicHubIndex>> GetPublicIndexAsync(
        Guid businessId, GetPublicHubIndexHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<PublicHubArticle>> GetPublicAsync(
        Guid businessId, string slug, GetPublicHubArticleHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, slug, cancellationToken));
}
