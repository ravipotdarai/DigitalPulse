using DigitalPulse.Application.Features.Auth;
using DigitalPulse.Application.Features.Connections;
using DigitalPulse.Application.Features.Businesses;
using DigitalPulse.Application.Features.Dashboard;
using DigitalPulse.Application.Features.Identity;
using DigitalPulse.Application.Features.Locations;
using DigitalPulse.Application.Features.Onboarding;
using DigitalPulse.Application.Features.Plans;
using DigitalPulse.Application.Features.Scans;
using DigitalPulse.Application.Features.Subscriptions;
using DigitalPulse.Application.Features.Tenants;
using DigitalPulse.Application.Features.Directories;
using DigitalPulse.Application.Features.Social;
using DigitalPulse.Application.Features.Website;
using DigitalPulse.Contracts.Auth;
using DigitalPulse.Contracts.Billing;
using DigitalPulse.Contracts.Businesses;
using DigitalPulse.Contracts.Directories;
using DigitalPulse.Contracts.Social;
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
        services.AddScoped<GetBusinessIdentityHandler>();
        services.AddScoped<UpdateBusinessProfileHandler>();
        services.AddScoped<AddContactPointHandler>();
        services.AddScoped<UpdateContactPointHandler>();
        services.AddScoped<DeleteOwnedHandler>();
        services.AddScoped<AddCategoryHandler>();
        services.AddScoped<UpdateCategoryHandler>();
        services.AddScoped<AddServiceHandler>();
        services.AddScoped<UpdateServiceHandler>();
        services.AddScoped<AddBusinessBrandHandler>();
        services.AddScoped<AddFactHandler>();
        services.AddScoped<UpdateFactHandler>();
        services.AddScoped<AddCustomerHandler>();
        services.AddScoped<UpdateCustomerHandler>();
        services.AddScoped<AddCustomerContactHandler>();
        services.AddScoped<GetConnectionCenterHandler>();
        services.AddScoped<StartConnectionHandler>();
        services.AddScoped<CompleteConnectionHandler>();
        services.AddScoped<CompleteConnectionByStateHandler>();
        services.AddScoped<ConnectionActionHandler>();
        services.AddScoped<GetScanCenterHandler>();
        services.AddScoped<GetScanHandler>();
        services.AddScoped<RunScanHandler>();
        services.AddScoped<UpdateFindingHandler>();
        services.AddScoped<GetWebsiteIntelligenceHandler>();
        services.AddScoped<AnalyzeWebsiteHandler>();
        services.AddScoped<SearchWebsiteHandler>();
        services.AddScoped<GetSocialWorkspaceHandler>();
        services.AddScoped<CreateSocialContentHandler>();
        services.AddScoped<UpdateSocialContentHandler>();
        services.AddScoped<ApproveSocialContentHandler>();
        services.AddScoped<PublishSocialContentHandler>();
        services.AddScoped<RefreshSocialMetricsHandler>();
        services.AddScoped<GetDirectoryWorkspaceHandler>();
        services.AddScoped<PrepareDirectoryTaskHandler>();
        services.AddScoped<CompleteDirectoryStepHandler>();
        services.AddScoped<VerifyDirectoryTaskHandler>();
        services.AddScoped<MonitorDirectoryHandler>();
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

public sealed class UpdateBusinessProfileRequestValidator : AbstractValidator<UpdateBusinessProfileRequest>
{
    public UpdateBusinessProfileRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Website).MaximumLength(2048);
        RuleFor(x => x.BrandVoice).MaximumLength(2000);
        RuleFor(x => x.IndustryCode).MaximumLength(32);
        RuleFor(x => x.FoundedYear).InclusiveBetween(1800, 2100).When(x => x.FoundedYear.HasValue);
    }
}

public sealed class ContactPointRequestValidator : AbstractValidator<ContactPointRequest>
{
    public ContactPointRequestValidator()
    {
        RuleFor(x => x.Kind).NotEmpty();
        RuleFor(x => x.Value).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.Label).MaximumLength(80);
    }
}

public sealed class NamedItemRequestValidator : AbstractValidator<NamedItemRequest>
{
    public NamedItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public sealed class FactRequestValidator : AbstractValidator<FactRequest>
{
    public FactRequestValidator()
    {
        RuleFor(x => x.FactTypeCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Value).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Status).NotEmpty();
    }
}

public sealed class CustomerRequestValidator : AbstractValidator<CustomerRequest>
{
    public CustomerRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Mobile).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class CustomerContactRequestValidator : AbstractValidator<CustomerContactRequest>
{
    public CustomerContactRequestValidator()
    {
        RuleFor(x => x.Kind).NotEmpty();
        RuleFor(x => x.Value).NotEmpty().MaximumLength(320);
    }
}

public sealed class CreateSocialContentRequestValidator : AbstractValidator<CreateSocialContentRequest>
{
    public CreateSocialContentRequestValidator()
    {
        RuleFor(x => x.PlatformCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
    }
}

public sealed class UpdateSocialContentRequestValidator : AbstractValidator<UpdateSocialContentRequest>
{
    public UpdateSocialContentRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
    }
}

public sealed class PrepareDirectoryRequestValidator : AbstractValidator<PrepareDirectoryRequest>
{
    public PrepareDirectoryRequestValidator()
    {
        RuleFor(x => x.PlatformCode).NotEmpty().MaximumLength(32);
    }
}

public sealed class VerifyDirectoryRequestValidator : AbstractValidator<VerifyDirectoryRequest>
{
    public VerifyDirectoryRequestValidator()
    {
        RuleFor(x => x.Note).NotEmpty().MaximumLength(500);
    }
}
