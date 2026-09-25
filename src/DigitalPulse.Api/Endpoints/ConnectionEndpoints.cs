using DigitalPulse.Application.Features.Connections;
using DigitalPulse.Contracts.Connections;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;

namespace DigitalPulse.Api.Endpoints;

public static class ConnectionEndpoints
{
    public static IEndpointRouteBuilder MapConnectionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/businesses/{businessId:guid}/connections").WithTags("Connections").RequireAuthorization();
        group.MapGet("/", ListAsync);
        group.MapPost("/", StartAsync);
        group.MapPost("/{connectionId:guid}/complete", CompleteAsync);
        group.MapPost("/{connectionId:guid}/health", HealthAsync);
        group.MapPost("/{connectionId:guid}/diagnose", DiagnoseAsync);
        group.MapPost("/{connectionId:guid}/reauthorize", ReauthorizeAsync);
        group.MapGet("/{connectionId:guid}/accounts", AccountsAsync);
        group.MapPost("/{connectionId:guid}/account", SelectAccountAsync);
        group.MapDelete("/{connectionId:guid}", DisconnectAsync);

        app.MapGet("/v1/connections/callback", CallbackAsync).WithTags("Connections").AllowAnonymous();
        return app;
    }

    private static async Task<Ok<ConnectionCenterResponse>> ListAsync(
        Guid businessId, GetConnectionCenterHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<StartConnectionResponse>> StartAsync(
        Guid businessId, StartConnectionRequest request, StartConnectionHandler handler, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PlatformCode))
        {
            throw Application.Common.AppException.Validation("Platform code is required.");
        }

        return TypedResults.Ok(await handler.Handle(businessId, request.PlatformCode, cancellationToken));
    }

    private static async Task<Ok<ConnectionResponse>> CompleteAsync(
        Guid businessId,
        Guid connectionId,
        CompleteConnectionRequest request,
        CompleteConnectionHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, connectionId, request.Code, cancellationToken));

    private static async Task<Ok<ConnectionResponse>> HealthAsync(
        Guid businessId, Guid connectionId, ConnectionActionHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.HealthAsync(businessId, connectionId, cancellationToken));

    private static async Task<Ok<IReadOnlyList<DiagnosticResponse>>> DiagnoseAsync(
        Guid businessId, Guid connectionId, ConnectionActionHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.DiagnoseAsync(businessId, connectionId, cancellationToken));

    private static async Task<Ok<StartConnectionResponse>> ReauthorizeAsync(
        Guid businessId, Guid connectionId, ConnectionActionHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.ReauthorizeAsync(businessId, connectionId, cancellationToken));

    private static async Task<Ok<IReadOnlyList<ConnectionAccountOption>>> AccountsAsync(
        Guid businessId, Guid connectionId, ConnectionActionHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.ListAccountsAsync(businessId, connectionId, cancellationToken));

    private static async Task<Ok<ConnectionResponse>> SelectAccountAsync(
        Guid businessId,
        Guid connectionId,
        SelectConnectionAccountRequest request,
        ConnectionActionHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.SelectAccountAsync(businessId, connectionId, request.ExternalAccount, cancellationToken));

    private static async Task<NoContent> DisconnectAsync(
        Guid businessId, Guid connectionId, ConnectionActionHandler handler, CancellationToken cancellationToken)
    {
        await handler.DisconnectAsync(businessId, connectionId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<RedirectHttpResult> CallbackAsync(
        string state,
        string? code,
        CompleteConnectionByStateHandler handler,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var platform = await handler.Handle(state, code, cancellationToken);
        var web = configuration["Public:WebOrigin"] ?? "http://localhost:5173";
        return TypedResults.Redirect($"{web.TrimEnd('/')}/app/connections?connected={platform}");
    }
}
