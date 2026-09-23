using DigitalPulse.Application.Features.Auth;
using DigitalPulse.Application.Features.Businesses;
using DigitalPulse.Application.Features.Dashboard;
using DigitalPulse.Application.Features.Locations;
using DigitalPulse.Application.Features.Onboarding;
using DigitalPulse.Application.Features.Plans;
using DigitalPulse.Application.Features.Subscriptions;
using DigitalPulse.Application.Features.Tenants;
using DigitalPulse.Contracts.Auth;
using DigitalPulse.Contracts.Billing;
using DigitalPulse.Contracts.Businesses;
using DigitalPulse.Contracts.Tenancy;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalPulse.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginUserHandler>();
        services.AddScoped<GetMeHandler>();
        services.AddScoped<UpdateProfileHandler>();
        services.AddScoped<CreateTenantHandler>();
        services.AddScoped<GetCurrentTenantHandler>();
        services.AddScoped<UpdateTenantHandler>();
        services.AddScoped<CreateBusinessHandler>();
        services.AddScoped<ListBusinessesHandler>();
        services.AddScoped<UpdateBusinessHandler>();
        services.AddScoped<CreateLocationHandler>();
        services.AddScoped<ListLocationsHandler>();
        services.AddScoped<UpdateLocationHandler>();
        services.AddScoped<ListPlansHandler>();
        services.AddScoped<SelectPlanHandler>();
        services.AddScoped<GetOnboardingStatusHandler>();
        services.AddScoped<GetDashboardHandler>();
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        return services;
    }
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Type).NotEmpty().Must(t => t is "Direct" or "Agency")
            .WithMessage("Type must be Direct or Agency.");
    }
}

public sealed class CreateBusinessRequestValidator : AbstractValidator<CreateBusinessRequest>
{
    public CreateBusinessRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Website).MaximumLength(2048);
    }
}

public sealed class CreateLocationRequestValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.CountryCode).MaximumLength(2);
    }
}

public sealed class UpdateTenantRequestValidator : AbstractValidator<UpdateTenantRequest>
{
    public UpdateTenantRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
    }
}

public sealed class UpdateBusinessRequestValidator : AbstractValidator<UpdateBusinessRequest>
{
    public UpdateBusinessRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Website).MaximumLength(2048);
    }
}

public sealed class SelectPlanRequestValidator : AbstractValidator<SelectPlanRequest>
{
    public SelectPlanRequestValidator()
    {
        RuleFor(x => x.PlanCode).NotEmpty().MaximumLength(32);
    }
}

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(120);
    }
}
