using DigitalPulse.Application.Features.Monitoring;
using DigitalPulse.Contracts.Monitoring;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DigitalPulse.Api.Endpoints;

public static class MonitoringEndpoints
{
    public static IEndpointRouteBuilder MapMonitoringEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/businesses/{businessId:guid}/monitoring").WithTags("Monitoring").RequireAuthorization();
        group.MapGet("/", GetAsync);
        group.MapPost("/runs", RunAsync);
        group.MapPost("/reports", AssembleAsync);
        group.MapPost("/reports/{reportId:guid}/decision", DecideAsync);
        group.MapPost("/competitors", AddCompetitorAsync);
        group.MapPost("/alerts/{alertId:guid}/acknowledge", AcknowledgeAsync);
        return app;
    }

    private static async Task<Ok<MonitoringWorkspaceResponse>> GetAsync(
        Guid businessId, GetMonitoringWorkspaceHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<MonitoringWorkspaceResponse>> RunAsync(
        Guid businessId, RunMonitoringHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<MonitoringWorkspaceResponse>> AssembleAsync(
        Guid businessId, AssemblePresenceReportHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<MonitoringWorkspaceResponse>> DecideAsync(
        Guid businessId,
        Guid reportId,
        RecordReportDecisionRequest request,
        RecordReportDecisionHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, reportId, request, cancellationToken));

    private static async Task<Ok<MonitoringWorkspaceResponse>> AddCompetitorAsync(
        Guid businessId, AddCompetitorRequest request, AddCompetitorHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));

    private static async Task<Ok<MonitoringWorkspaceResponse>> AcknowledgeAsync(
        Guid businessId, Guid alertId, AcknowledgeAlertHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, alertId, cancellationToken));
}
