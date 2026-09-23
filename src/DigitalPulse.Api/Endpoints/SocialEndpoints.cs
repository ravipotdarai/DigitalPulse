using DigitalPulse.Application.Features.Social;
using DigitalPulse.Contracts.Social;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DigitalPulse.Api.Endpoints;

public static class SocialEndpoints
{
    public static IEndpointRouteBuilder MapSocialEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/businesses/{businessId:guid}/social").WithTags("Social").RequireAuthorization();
        group.MapGet("/", GetAsync);
        group.MapPost("/content", CreateAsync);
        group.MapPut("/content/{contentId:guid}", UpdateAsync);
        group.MapPost("/content/{contentId:guid}/approve", ApproveAsync);
        group.MapPost("/content/{contentId:guid}/publish", PublishAsync);
        group.MapPost("/metrics/refresh", RefreshAsync);
        return app;
    }

    private static async Task<Ok<SocialWorkspaceResponse>> GetAsync(
        Guid businessId, GetSocialWorkspaceHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<SocialContentResponse>> CreateAsync(
        Guid businessId, CreateSocialContentRequest request, CreateSocialContentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));

    private static async Task<Ok<SocialContentResponse>> UpdateAsync(
        Guid businessId, Guid contentId, UpdateSocialContentRequest request, UpdateSocialContentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, contentId, request, cancellationToken));

    private static async Task<Ok<SocialContentResponse>> ApproveAsync(
        Guid businessId, Guid contentId, ApproveSocialContentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, contentId, cancellationToken));

    private static async Task<Ok<SocialContentResponse>> PublishAsync(
        Guid businessId, Guid contentId, PublishSocialContentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, contentId, cancellationToken));

    private static async Task<Ok<SocialWorkspaceResponse>> RefreshAsync(
        Guid businessId, RefreshSocialMetricsHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));
}
