using DigitalPulse.Application.Features.Businesses;
using DigitalPulse.Application.Features.Locations;
using DigitalPulse.Contracts.Businesses;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DigitalPulse.Api.Endpoints;

public static class BusinessEndpoints
{
    public static IEndpointRouteBuilder MapBusinessEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/businesses").WithTags("Businesses").RequireAuthorization();
        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{businessId:guid}", UpdateAsync);
        group.MapGet("/{businessId:guid}/locations", ListLocationsAsync);
        group.MapPost("/{businessId:guid}/locations", CreateLocationAsync);
        group.MapPut("/{businessId:guid}/locations/{locationId:guid}", UpdateLocationAsync);
        return app;
    }

    private static async Task<Ok<IReadOnlyList<BusinessResponse>>> ListAsync(
        ListBusinessesHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(cancellationToken));

    private static async Task<Ok<BusinessResponse>> CreateAsync(
        CreateBusinessRequest request,
        IValidator<CreateBusinessRequest> validator,
        CreateBusinessHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(request, cancellationToken));
    }

    private static async Task<Ok<BusinessResponse>> UpdateAsync(
        Guid businessId,
        UpdateBusinessRequest request,
        IValidator<UpdateBusinessRequest> validator,
        UpdateBusinessHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));
    }

    private static async Task<Ok<IReadOnlyList<LocationResponse>>> ListLocationsAsync(
        Guid businessId,
        ListLocationsHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<LocationResponse>> CreateLocationAsync(
        Guid businessId,
        CreateLocationRequest request,
        IValidator<CreateLocationRequest> validator,
        CreateLocationHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));
    }

    private static async Task<Ok<LocationResponse>> UpdateLocationAsync(
        Guid businessId,
        Guid locationId,
        CreateLocationRequest request,
        IValidator<CreateLocationRequest> validator,
        UpdateLocationHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(businessId, locationId, request, cancellationToken));
    }
}
