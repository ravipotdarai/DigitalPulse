using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Features.Billing;
using DigitalPulse.Contracts.Billing;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DigitalPulse.Api.Endpoints;

public static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/billing").WithTags("Billing");
        group.MapGet("/", GetAsync).RequireAuthorization();
        group.MapPost("/change-plan", ChangeAsync).RequireAuthorization();
        group.MapPost("/checkout", CheckoutAsync).RequireAuthorization();
        group.MapPost("/cancel", CancelAsync).RequireAuthorization();
        group.MapPost("/resume", ResumeAsync).RequireAuthorization();
        group.MapPost("/webhooks", WebhookAsync).AllowAnonymous();
        return app;
    }

    private static async Task<Ok<BillingWorkspaceResponse>> GetAsync(
        GetBillingWorkspaceHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(cancellationToken));

    private static async Task<Ok<BillingWorkspaceResponse>> ChangeAsync(
        ChangePlanRequest request, ChangePlanHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(request, cancellationToken));

    private static async Task<Ok<BillingWorkspaceResponse>> CheckoutAsync(
        CheckoutRequest request, CheckoutBillingHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(request, cancellationToken));

    private static async Task<Ok<BillingWorkspaceResponse>> CancelAsync(
        CancelSubscriptionRequest request, CancelSubscriptionHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(request, cancellationToken));

    private static async Task<Ok<BillingWorkspaceResponse>> ResumeAsync(
        ResumeSubscriptionHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(cancellationToken));

    private static async Task<Ok<BillingWebhookResponse>> WebhookAsync(
        BillingWebhookRequest request,
        HttpRequest http,
        RecordBillingWebhookHandler handler,
        CancellationToken cancellationToken)
    {
        var signature = string.IsNullOrWhiteSpace(request.Signature)
            ? http.Headers["X-Razorpay-Signature"].ToString()
            : request.Signature;
        return TypedResults.Ok(await handler.Handle(
            new BillingWebhookIngest(signature, request.Payload ?? string.Empty, request.EventType),
            cancellationToken));
    }
}

public sealed record BillingWebhookRequest(string? EventType, string? Payload, string? Signature);
