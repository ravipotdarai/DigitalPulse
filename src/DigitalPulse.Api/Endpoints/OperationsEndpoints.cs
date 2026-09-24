using DigitalPulse.Application.Features.Operations;
using DigitalPulse.Contracts.Operations;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DigitalPulse.Api.Endpoints;

public static class OperationsEndpoints
{
    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/operations").WithTags("Operations").RequireAuthorization();
        group.MapGet("/", GetAsync);
        group.MapPost("/backups", BackupAsync);
        group.MapPost("/backups/{snapshotId:guid}/restore", RestoreAsync);
        group.MapPost("/drills", DrillAsync);
        group.MapPost("/scans", ScanAsync);
        group.MapPost("/readiness", ReadinessAsync);
        return app;
    }

    private static async Task<Ok<OperationsWorkspaceResponse>> GetAsync(
        GetOperationsWorkspaceHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(cancellationToken));

    private static async Task<Ok<OperationsWorkspaceResponse>> BackupAsync(
        CreateBackupHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(cancellationToken));

    private static async Task<Ok<OperationsWorkspaceResponse>> RestoreAsync(
        Guid snapshotId,
        RestoreBackupHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(snapshotId, cancellationToken));

    private static async Task<Ok<OperationsWorkspaceResponse>> DrillAsync(
        StartDisasterDrillRequest request,
        IValidator<StartDisasterDrillRequest> validator,
        StartDisasterDrillHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(request, cancellationToken));
    }

    private static async Task<Ok<OperationsWorkspaceResponse>> ScanAsync(
        RunInventoryRequest request,
        RunInventoryHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(request, cancellationToken));

    private static async Task<Ok<OperationsWorkspaceResponse>> ReadinessAsync(
        AssembleReadinessHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(cancellationToken));
}
