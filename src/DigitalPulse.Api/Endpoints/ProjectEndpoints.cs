using DigitalPulse.Application.Features.Projects;
using DigitalPulse.Contracts.Projects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DigitalPulse.Api.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/businesses/{businessId:guid}/projects").WithTags("Projects").RequireAuthorization();
        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);
        group.MapGet("/{projectId:guid}", GetAsync);
        group.MapPut("/{projectId:guid}", UpdateAsync);
        group.MapPost("/{projectId:guid}/services", LinkServiceAsync);
        group.MapPost("/{projectId:guid}/brands", LinkBrandAsync);
        group.MapPost("/{projectId:guid}/media", RegisterMediaAsync);
        group.MapPost("/{projectId:guid}/factory", FactoryAsync);
        group.MapPost("/{projectId:guid}/content/{contentId:guid}/approvals", RequestApprovalAsync);
        group.MapPost("/{projectId:guid}/approvals/{approvalId:guid}/decide", DecideAsync);
        return app;
    }

    private static async Task<Ok<ProjectWorkspaceResponse>> ListAsync(
        Guid businessId, GetProjectWorkspaceHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<ProjectDetailResponse>> CreateAsync(
        Guid businessId, CreateProjectRequest request, CreateProjectHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));

    private static async Task<Ok<ProjectDetailResponse>> GetAsync(
        Guid businessId, Guid projectId, GetProjectHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, projectId, cancellationToken));

    private static async Task<Ok<ProjectDetailResponse>> UpdateAsync(
        Guid businessId, Guid projectId, UpdateProjectRequest request, UpdateProjectHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, projectId, request, cancellationToken));

    private static async Task<Ok<ProjectDetailResponse>> LinkServiceAsync(
        Guid businessId, Guid projectId, LinkNamedRequest request, LinkProjectServiceHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, projectId, request, cancellationToken));

    private static async Task<Ok<ProjectDetailResponse>> LinkBrandAsync(
        Guid businessId, Guid projectId, LinkNamedRequest request, LinkProjectBrandHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, projectId, request, cancellationToken));

    private static async Task<Ok<ProjectDetailResponse>> RegisterMediaAsync(
        Guid businessId, Guid projectId, RegisterMediaRequest request, RegisterProjectMediaHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, projectId, request, cancellationToken));

    private static async Task<Ok<ProjectDetailResponse>> FactoryAsync(
        Guid businessId, Guid projectId, GenerateProjectContentHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, projectId, cancellationToken));

    private static async Task<Ok<ProjectDetailResponse>> RequestApprovalAsync(
        Guid businessId, Guid projectId, Guid contentId, RequestContentApprovalHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, projectId, contentId, cancellationToken));

    private static async Task<Ok<ProjectDetailResponse>> DecideAsync(
        Guid businessId, Guid projectId, Guid approvalId, DecideApprovalRequest request, DecideContentApprovalHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, projectId, approvalId, request, cancellationToken));
}
