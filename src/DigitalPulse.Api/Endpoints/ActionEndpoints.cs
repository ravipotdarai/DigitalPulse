using DigitalPulse.Application.Features.Actions;
using DigitalPulse.Contracts.Actions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DigitalPulse.Api.Endpoints;

public static class ActionEndpoints
{
    public static IEndpointRouteBuilder MapActionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/businesses/{businessId:guid}/actions").WithTags("Actions").RequireAuthorization();
        group.MapGet("/", GetAsync);
        group.MapPut("/policy", UpdatePolicyAsync);
        group.MapPost("/", EnqueueAsync);
        group.MapPost("/{actionId:guid}/approve", ApproveAsync);
        group.MapPost("/{actionId:guid}/execute", ExecuteAsync);
        group.MapPost("/{actionId:guid}/retry", RetryAsync);
        group.MapPost("/{actionId:guid}/verify", VerifyAsync);
        return app;
    }

    private static async Task<Ok<ActionWorkspaceResponse>> GetAsync(
        Guid businessId, GetActionWorkspaceHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<ActionWorkspaceResponse>> UpdatePolicyAsync(
        Guid businessId, UpdateAutomationPolicyRequest request, UpdateAutomationPolicyHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));

    private static async Task<Ok<WorkActionResponse>> EnqueueAsync(
        Guid businessId, EnqueueActionRequest request, EnqueueActionHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));

    private static async Task<Ok<WorkActionResponse>> ApproveAsync(
        Guid businessId, Guid actionId, ApproveActionHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, actionId, cancellationToken));

    private static async Task<Ok<WorkActionResponse>> ExecuteAsync(
        Guid businessId, Guid actionId, ExecuteActionHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, actionId, cancellationToken));

    private static async Task<Ok<WorkActionResponse>> RetryAsync(
        Guid businessId, Guid actionId, RetryActionHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, actionId, cancellationToken));

    private static async Task<Ok<WorkActionResponse>> VerifyAsync(
        Guid businessId, Guid actionId, VerifyActionHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, actionId, cancellationToken));
}
