using DigitalPulse.Application.Features.Auth;
using DigitalPulse.Contracts.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DigitalPulse.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/auth").WithTags("Auth");
        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapGet("/me", MeAsync).RequireAuthorization();
        group.MapPut("/me", UpdateMeAsync).RequireAuthorization();
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        return app;
    }

    private static async Task<Ok<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        IValidator<RegisterRequest> validator,
        RegisterUserHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(request, cancellationToken));
    }

    private static async Task<Ok<AuthResponse>> LoginAsync(
        LoginRequest request,
        IValidator<LoginRequest> validator,
        LoginUserHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(request, cancellationToken));
    }

    private static async Task<Ok<MeResponse>> MeAsync(GetMeHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(cancellationToken));

    private static async Task<Ok<MeResponse>> UpdateMeAsync(
        UpdateProfileRequest request,
        IValidator<UpdateProfileRequest> validator,
        UpdateProfileHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(request, cancellationToken));
    }

    private static IResult LogoutAsync() => TypedResults.NoContent();
}
