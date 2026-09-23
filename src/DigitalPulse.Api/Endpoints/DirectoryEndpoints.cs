using DigitalPulse.Application.Features.Directories;
using DigitalPulse.Contracts.Directories;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DigitalPulse.Api.Endpoints;

public static class DirectoryEndpoints
{
    public static IEndpointRouteBuilder MapDirectoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/businesses/{businessId:guid}/directories").WithTags("Directories").RequireAuthorization();
        group.MapGet("/", GetAsync);
        group.MapPost("/prepare", PrepareAsync);
        group.MapPost("/{platformCode}/monitor", MonitorAsync);
        group.MapPost("/tasks/{taskId:guid}/steps/{stepId:guid}/complete", CompleteAsync);
        group.MapPost("/tasks/{taskId:guid}/verify", VerifyAsync);
        return app;
    }

    private static async Task<Ok<DirectoryWorkspaceResponse>> GetAsync(
        Guid businessId, GetDirectoryWorkspaceHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<DirectoryTaskResponse>> PrepareAsync(
        Guid businessId, PrepareDirectoryRequest request, PrepareDirectoryTaskHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));

    private static async Task<Ok<DirectoryWorkspaceResponse>> MonitorAsync(
        Guid businessId, string platformCode, MonitorDirectoryHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, platformCode, cancellationToken));

    private static async Task<Ok<DirectoryTaskResponse>> CompleteAsync(
        Guid businessId, Guid taskId, Guid stepId, CompleteDirectoryStepHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, taskId, stepId, cancellationToken));

    private static async Task<Ok<DirectoryTaskResponse>> VerifyAsync(
        Guid businessId, Guid taskId, VerifyDirectoryRequest request, VerifyDirectoryTaskHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, taskId, request, cancellationToken));
}
