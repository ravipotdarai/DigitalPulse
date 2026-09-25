using DigitalPulse.Application.Features.Website;
using DigitalPulse.Contracts.Website;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DigitalPulse.Api.Endpoints;

public static class WebsiteEndpoints
{
    public static IEndpointRouteBuilder MapWebsiteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/businesses/{businessId:guid}/website").WithTags("Website").RequireAuthorization();
        group.MapGet("/", GetAsync);
        group.MapPost("/analyze", AnalyzeAsync);
        group.MapGet("/search", SearchAsync);

        var reports = app.MapGroup("/v1/businesses/{businessId:guid}/reports").WithTags("Reports").RequireAuthorization();
        reports.MapGet("/", ListReportsAsync);
        reports.MapGet("/{reportId:guid}/pdf", PdfAsync);
        return app;
    }

    private static async Task<Ok<WebsiteIntelligenceResponse>> GetAsync(
        Guid businessId, GetWebsiteIntelligenceHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<WebsiteIntelligenceResponse>> AnalyzeAsync(
        Guid businessId, AnalyzeWebsiteHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<SiteSearchResponse>> SearchAsync(
        Guid businessId, string? q, SearchWebsiteHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, q, cancellationToken));

    private static async Task<Ok<IReadOnlyList<TestReportResponse>>> ListReportsAsync(
        Guid businessId, ListTestReportsHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<IResult> PdfAsync(
        Guid businessId, Guid reportId, DownloadTestReportPdfHandler handler, CancellationToken cancellationToken)
    {
        var (fileName, bytes) = await handler.Handle(businessId, reportId, cancellationToken);
        return Results.File(bytes, "application/pdf", fileName);
    }
}
