using DigitalPulse.Application.Features.Tenants;
using DigitalPulse.Contracts.Auth;
using DigitalPulse.Contracts.Tenancy;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DigitalPulse.Api.Endpoints;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/tenants").WithTags("Tenants").RequireAuthorization();
        group.MapPost("/", CreateAsync);
        group.MapGet("/current", GetCurrentAsync);
        group.MapPut("/current", UpdateCurrentAsync);
        return app;
    }

    private static async Task<Ok<AuthResponse>> CreateAsync(
        CreateTenantRequest request,
        IValidator<CreateTenantRequest> validator,
        CreateTenantHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(request, cancellationToken));
    }

    private static async Task<Ok<TenantResponse>> GetCurrentAsync(
        GetCurrentTenantHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(cancellationToken));

    private static async Task<Ok<TenantResponse>> UpdateCurrentAsync(
        UpdateTenantRequest request,
        IValidator<UpdateTenantRequest> validator,
        UpdateTenantHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(request, cancellationToken));
    }
}
