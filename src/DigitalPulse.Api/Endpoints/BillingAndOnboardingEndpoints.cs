using DigitalPulse.Application.Features.Dashboard;
using DigitalPulse.Application.Features.Onboarding;
using DigitalPulse.Application.Features.Plans;
using DigitalPulse.Application.Features.Subscriptions;
using DigitalPulse.Contracts.Billing;
using DigitalPulse.Contracts.Onboarding;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DigitalPulse.Api.Endpoints;

public static class BillingAndOnboardingEndpoints
{
    public static IEndpointRouteBuilder MapBillingAndOnboardingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/plans", ListPlansAsync).WithTags("Plans").RequireAuthorization();
        app.MapPost("/v1/subscriptions", SelectPlanAsync).WithTags("Plans").RequireAuthorization();
        app.MapGet("/v1/onboarding", OnboardingAsync).WithTags("Onboarding").RequireAuthorization();
        app.MapGet("/v1/dashboard", DashboardAsync).WithTags("Dashboard").RequireAuthorization();
        return app;
    }

    private static async Task<Ok<IReadOnlyList<PlanResponse>>> ListPlansAsync(
        ListPlansHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(cancellationToken));

    private static async Task<Ok<SubscriptionResponse>> SelectPlanAsync(
        SelectPlanRequest request,
        IValidator<SelectPlanRequest> validator,
        SelectPlanHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(request, cancellationToken));
    }

    private static async Task<Ok<OnboardingStatusResponse>> OnboardingAsync(
        GetOnboardingStatusHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(cancellationToken));

    private static async Task<Ok<DashboardResponse>> DashboardAsync(
        GetDashboardHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(cancellationToken));
}
