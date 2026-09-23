using DigitalPulse.Application.Features.Scans;
using DigitalPulse.Contracts.Scans;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DigitalPulse.Api.Endpoints;

public static class ScanEndpoints
{
    public static IEndpointRouteBuilder MapScanEndpoints(this IEndpointRouteBuilder app)
    {
        var scans = app.MapGroup("/v1/businesses/{businessId:guid}/scans").WithTags("Scans").RequireAuthorization();
        scans.MapGet("/", ListAsync);
        scans.MapPost("/", RunAsync);
        scans.MapGet("/{scanId:guid}", GetAsync);

        var findings = app.MapGroup("/v1/businesses/{businessId:guid}/findings").WithTags("Findings").RequireAuthorization();
        findings.MapPatch("/{findingId:guid}", UpdateAsync);
        return app;
    }

    private static async Task<Ok<ScanCenterResponse>> ListAsync(
        Guid businessId, GetScanCenterHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<ScanDetailResponse>> RunAsync(
        Guid businessId, RunScanHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<ScanDetailResponse>> GetAsync(
        Guid businessId, Guid scanId, GetScanHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, scanId, cancellationToken));

    private static async Task<Ok<FindingResponse>> UpdateAsync(
        Guid businessId,
        Guid findingId,
        UpdateFindingRequest request,
        UpdateFindingHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, findingId, request, cancellationToken));
}
