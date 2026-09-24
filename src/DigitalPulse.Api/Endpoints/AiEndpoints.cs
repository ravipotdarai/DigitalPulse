using DigitalPulse.Application.Features.Ai;
using DigitalPulse.Contracts.Ai;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DigitalPulse.Api.Endpoints;

public static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/businesses/{businessId:guid}/ai").WithTags("AI").RequireAuthorization();
        group.MapGet("/", GetAsync);
        group.MapPost("/knowledge", AddKnowledgeAsync);
        group.MapPost("/graph/sync", SyncAsync);
        group.MapPost("/runs", RunAsync);
        return app;
    }

    private static async Task<Ok<AiWorkspaceResponse>> GetAsync(
        Guid businessId, GetAiWorkspaceHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<KnowledgeEntryResponse>> AddKnowledgeAsync(
        Guid businessId, AddKnowledgeRequest request, AddKnowledgeHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));

    private static async Task<Ok<AiWorkspaceResponse>> SyncAsync(
        Guid businessId, SyncGraphHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<AiRunResponse>> RunAsync(
        Guid businessId, RunAiRequest request, RunAiHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));
}
