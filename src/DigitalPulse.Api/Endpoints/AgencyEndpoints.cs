using DigitalPulse.Application.Features.Agency;
using DigitalPulse.Contracts.Agency;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DigitalPulse.Api.Endpoints;

public static class AgencyEndpoints
{
    public static IEndpointRouteBuilder MapAgencyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/agency").WithTags("Agency").RequireAuthorization();
        group.MapGet("/", GetAsync);
        group.MapPost("/clients", CreateClientAsync);
        group.MapPut("/clients/{clientId:guid}", UpdateClientAsync);
        group.MapPut("/white-label", UpdateWhiteLabelAsync);
        group.MapPost("/workflows", StartWorkflowAsync);
        group.MapPost("/workflows/{workflowId:guid}/advance", AdvanceWorkflowAsync);
        group.MapPost("/reports", AssembleReportAsync);
        group.MapPost("/reports/{reportId:guid}/decision", RecordDecisionAsync);
        return app;
    }

    private static async Task<Ok<AgencyWorkspaceResponse>> GetAsync(
        GetAgencyWorkspaceHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(cancellationToken));

    private static async Task<Ok<AgencyWorkspaceResponse>> CreateClientAsync(
        CreateAgencyClientRequest request,
        IValidator<CreateAgencyClientRequest> validator,
        CreateAgencyClientHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(request, cancellationToken));
    }

    private static async Task<Ok<AgencyWorkspaceResponse>> UpdateClientAsync(
        Guid clientId,
        UpdateAgencyClientRequest request,
        IValidator<UpdateAgencyClientRequest> validator,
        UpdateAgencyClientHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(clientId, request, cancellationToken));
    }

    private static async Task<Ok<AgencyWorkspaceResponse>> UpdateWhiteLabelAsync(
        UpdateWhiteLabelRequest request,
        IValidator<UpdateWhiteLabelRequest> validator,
        UpdateWhiteLabelHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(request, cancellationToken));
    }

    private static async Task<Ok<AgencyWorkspaceResponse>> StartWorkflowAsync(
        StartAgencyWorkflowRequest request,
        IValidator<StartAgencyWorkflowRequest> validator,
        StartAgencyWorkflowHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(request, cancellationToken));
    }

    private static async Task<Ok<AgencyWorkspaceResponse>> AdvanceWorkflowAsync(
        Guid workflowId,
        AdvanceAgencyWorkflowRequest request,
        AdvanceAgencyWorkflowHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(workflowId, request, cancellationToken));

    private static async Task<Ok<AgencyWorkspaceResponse>> AssembleReportAsync(
        AssembleAgencyReportRequest request,
        AssembleAgencyReportHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(request, cancellationToken));

    private static async Task<Ok<AgencyWorkspaceResponse>> RecordDecisionAsync(
        Guid reportId,
        RecordAgencyDecisionRequest request,
        IValidator<RecordAgencyDecisionRequest> validator,
        RecordAgencyReportDecisionHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(reportId, request, cancellationToken));
    }
}
