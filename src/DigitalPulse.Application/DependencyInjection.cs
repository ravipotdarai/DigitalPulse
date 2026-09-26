using DigitalPulse.Application.Features.Actions;
using DigitalPulse.Application.Features.Ai;
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
using DigitalPulse.Application.Features.Content;
using DigitalPulse.Application.Features.Projects;
using DigitalPulse.Application.Features.Social;
using DigitalPulse.Application.Features.Website;
using DigitalPulse.Application.Features.WhatsApp;
using DigitalPulse.Application.Features.Monitoring;
using DigitalPulse.Application.Features.Billing;
using DigitalPulse.Application.Features.Agency;
using DigitalPulse.Application.Features.Operations;
using DigitalPulse.Contracts.WhatsApp;
using DigitalPulse.Contracts.Monitoring;
using DigitalPulse.Contracts.Actions;
using DigitalPulse.Contracts.Ai;
using DigitalPulse.Contracts.Auth;
using DigitalPulse.Contracts.Agency;
using DigitalPulse.Contracts.Operations;
using DigitalPulse.Contracts.Billing;
using DigitalPulse.Contracts.Businesses;
using DigitalPulse.Contracts.Directories;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Contracts.Projects;
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
        services.AddScoped<CompleteFindingStepHandler>();
        services.AddScoped<VerifyFindingHandler>();
        services.AddScoped<GetWebsiteIntelligenceHandler>();
        services.AddScoped<AnalyzeWebsiteHandler>();
        services.AddScoped<SearchWebsiteHandler>();
        services.AddScoped<ListTestReportsHandler>();
        services.AddScoped<DownloadTestReportPdfHandler>();
        services.AddScoped<SearchConsoleWriteHandler>();
        services.AddScoped<GetSocialWorkspaceHandler>();
        services.AddScoped<CreateSocialContentHandler>();
        services.AddScoped<UpdateSocialContentHandler>();
        services.AddScoped<ApproveSocialContentHandler>();
        services.AddScoped<DeleteSocialContentHandler>();
        services.AddScoped<PublishSocialContentHandler>();
        services.AddScoped<UploadSocialMediaHandler>();
        services.AddScoped<RefreshSocialMetricsHandler>();
        services.AddScoped<GetDirectoryWorkspaceHandler>();
        services.AddScoped<PrepareDirectoryTaskHandler>();
        services.AddScoped<CompleteDirectoryStepHandler>();
        services.AddScoped<VerifyDirectoryTaskHandler>();
        services.AddScoped<MonitorDirectoryHandler>();
        services.AddScoped<GetProjectWorkspaceHandler>();
        services.AddScoped<GetProjectHandler>();
        services.AddScoped<CreateProjectHandler>();
        services.AddScoped<UpdateProjectHandler>();
        services.AddScoped<LinkProjectServiceHandler>();
        services.AddScoped<LinkProjectBrandHandler>();
        services.AddScoped<RegisterProjectMediaHandler>();
        services.AddScoped<GenerateProjectContentHandler>();
        services.AddScoped<RequestContentApprovalHandler>();
        services.AddScoped<DecideContentApprovalHandler>();
        services.AddScoped<GetContentHubHandler>();
        services.AddScoped<GetHubContentHandler>();
        services.AddScoped<CreateHubContentHandler>();
        services.AddScoped<UpdateHubContentHandler>();
        services.AddScoped<DeleteHubContentHandler>();
        services.AddScoped<ApproveHubContentHandler>();
        services.AddScoped<RejectHubContentHandler>();
        services.AddScoped<ScheduleHubContentHandler>();
        services.AddScoped<CancelHubScheduleHandler>();
        services.AddScoped<PublishHubContentHandler>();
        services.AddScoped<ArchiveHubContentHandler>();
        services.AddScoped<RestoreHubRevisionHandler>();
        services.AddScoped<AttachHubMediaHandler>();
        services.AddScoped<RegisterHubMediaHandler>();
        services.AddScoped<ReleaseScheduledHubContentHandler>();
        services.AddScoped<AnalyzeHubSeoHandler>();
        services.AddScoped<CreateHubVariantsHandler>();
        services.AddScoped<DistributeHubContentHandler>();
        services.AddScoped<DiscoverContentOpportunitiesHandler>();
        services.AddScoped<GenerateHubContentHandler>();
        services.AddScoped<GetPublicHubIndexHandler>();
        services.AddScoped<GetPublicHubArticleHandler>();
        services.AddScoped<GetAiWorkspaceHandler>();
        services.AddScoped<AddKnowledgeHandler>();
        services.AddScoped<SyncGraphHandler>();
        services.AddScoped<RunAiHandler>();
        services.AddScoped<GetActionWorkspaceHandler>();
        services.AddScoped<UpdateAutomationPolicyHandler>();
        services.AddScoped<EnqueueActionHandler>();
        services.AddScoped<ApproveActionHandler>();
        services.AddScoped<ExecuteActionHandler>();
        services.AddScoped<RetryActionHandler>();
        services.AddScoped<VerifyActionHandler>();
        services.AddScoped<GetWhatsAppWorkspaceHandler>();
        services.AddScoped<ConnectWhatsAppHandler>();
        services.AddScoped<VerifyWhatsAppPhoneHandler>();
        services.AddScoped<ImportWhatsAppContactsHandler>();
        services.AddScoped<SetWhatsAppConsentHandler>();
        services.AddScoped<CreateWhatsAppTemplateHandler>();
        services.AddScoped<ApproveWhatsAppTemplateHandler>();
        services.AddScoped<CreateWhatsAppCampaignHandler>();
        services.AddScoped<ApproveWhatsAppCampaignHandler>();
        services.AddScoped<ScheduleWhatsAppCampaignHandler>();
        services.AddScoped<DraftWhatsAppMessageHandler>();
        services.AddScoped<DraftWhatsAppAiHandler>();
        services.AddScoped<ApproveWhatsAppMessageHandler>();
        services.AddScoped<SendWhatsAppMessageHandler>();
        services.AddScoped<RecordWhatsAppInboundHandler>();
        services.AddScoped<ReplyWhatsAppHandler>();
        services.AddScoped<GetMonitoringWorkspaceHandler>();
        services.AddScoped<RunMonitoringHandler>();
        services.AddScoped<AssemblePresenceReportHandler>();
        services.AddScoped<AddCompetitorHandler>();
        services.AddScoped<AcknowledgeAlertHandler>();
        services.AddScoped<RecordReportDecisionHandler>();
        services.AddScoped<GetBillingWorkspaceHandler>();
        services.AddScoped<ChangePlanHandler>();
        services.AddScoped<CheckoutBillingHandler>();
        services.AddScoped<CancelSubscriptionHandler>();
        services.AddScoped<ResumeSubscriptionHandler>();
        services.AddScoped<RecordBillingWebhookHandler>();
        services.AddScoped<GetAgencyWorkspaceHandler>();
        services.AddScoped<CreateAgencyClientHandler>();
        services.AddScoped<UpdateAgencyClientHandler>();
        services.AddScoped<UpdateWhiteLabelHandler>();
        services.AddScoped<StartAgencyWorkflowHandler>();
        services.AddScoped<AdvanceAgencyWorkflowHandler>();
        services.AddScoped<AssembleAgencyReportHandler>();
        services.AddScoped<RecordAgencyReportDecisionHandler>();
        services.AddScoped<GetOperationsWorkspaceHandler>();
        services.AddScoped<CreateBackupHandler>();
        services.AddScoped<RestoreBackupHandler>();
        services.AddScoped<StartDisasterDrillHandler>();
        services.AddScoped<RunInventoryHandler>();
        services.AddScoped<AssembleReadinessHandler>();
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

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.PermissionScope).NotEmpty();
        RuleFor(x => x.Confidentiality).NotEmpty();
    }
}

public sealed class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.PermissionScope).NotEmpty();
        RuleFor(x => x.Confidentiality).NotEmpty();
        RuleFor(x => x.PublicationStatus).NotEmpty();
    }
}

public sealed class RegisterMediaRequestValidator : AbstractValidator<RegisterMediaRequest>
{
    public RegisterMediaRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Kind).NotEmpty();
        RuleFor(x => x.SourceUrl).MaximumLength(2048);
    }
}

public sealed class DecideApprovalRequestValidator : AbstractValidator<DecideApprovalRequest>
{
    public DecideApprovalRequestValidator()
    {
        RuleFor(x => x.Note).NotEmpty().MaximumLength(500);
    }
}

public sealed class AddKnowledgeRequestValidator : AbstractValidator<AddKnowledgeRequest>
{
    public AddKnowledgeRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Kind).NotEmpty();
        RuleFor(x => x.SourceUrl).MaximumLength(2048);
    }
}

public sealed class UpdateAutomationPolicyRequestValidator : AbstractValidator<UpdateAutomationPolicyRequest>
{
    public UpdateAutomationPolicyRequestValidator()
    {
        RuleFor(x => x.Mode).NotEmpty();
        RuleFor(x => x.MaxAttempts).InclusiveBetween(1, 8);
    }
}

public sealed class EnqueueActionRequestValidator : AbstractValidator<EnqueueActionRequest>
{
    public EnqueueActionRequestValidator()
    {
        RuleFor(x => x.Kind).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(160);
        RuleFor(x => x.TargetLabel).MaximumLength(160);
    }
}

public sealed class RunAiRequestValidator : AbstractValidator<RunAiRequest>
{
    public RunAiRequestValidator()
    {
        RuleFor(x => x.Agent).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Prompt).NotEmpty().MinimumLength(4).MaximumLength(4000);
    }
}

public sealed class ConnectWhatsAppRequestValidator : AbstractValidator<ConnectWhatsAppRequest>
{
    public ConnectWhatsAppRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(32);
    }
}

public sealed class CreateWhatsAppTemplateRequestValidator : AbstractValidator<CreateWhatsAppTemplateRequest>
{
    public CreateWhatsAppTemplateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Body).NotEmpty().MinimumLength(3).MaximumLength(1024);
    }
}

public sealed class DraftWhatsAppMessageRequestValidator : AbstractValidator<DraftWhatsAppMessageRequest>
{
    public DraftWhatsAppMessageRequestValidator()
    {
        RuleFor(x => x.ContactId).NotEmpty();
        RuleFor(x => x.Kind).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MinimumLength(3).MaximumLength(4000);
    }
}

public sealed class AddCompetitorRequestValidator : AbstractValidator<AddCompetitorRequest>
{
    public AddCompetitorRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Website).MaximumLength(2048);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class RecordReportDecisionRequestValidator : AbstractValidator<RecordReportDecisionRequest>
{
    public RecordReportDecisionRequestValidator()
    {
        RuleFor(x => x.Decision).NotEmpty().MaximumLength(500);
    }
}

public sealed class CancelSubscriptionRequestValidator : AbstractValidator<CancelSubscriptionRequest>
{
    public CancelSubscriptionRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class ChangePlanRequestValidator : AbstractValidator<ChangePlanRequest>
{
    public ChangePlanRequestValidator()
    {
        RuleFor(x => x.PlanCode).NotEmpty().MaximumLength(32);
    }
}

public sealed class CreateAgencyClientRequestValidator : AbstractValidator<CreateAgencyClientRequest>
{
    public CreateAgencyClientRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Website).MaximumLength(2048);
        RuleFor(x => x.ContactName).MaximumLength(160);
        RuleFor(x => x.ContactEmail).MaximumLength(256);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.ExternalRef).MaximumLength(80);
    }
}

public sealed class UpdateAgencyClientRequestValidator : AbstractValidator<UpdateAgencyClientRequest>
{
    public UpdateAgencyClientRequestValidator()
    {
        RuleFor(x => x.Status).NotEmpty().MaximumLength(16);
        RuleFor(x => x.ContactName).MaximumLength(160);
        RuleFor(x => x.ContactEmail).MaximumLength(256);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.ExternalRef).MaximumLength(80);
    }
}

public sealed class UpdateWhiteLabelRequestValidator : AbstractValidator<UpdateWhiteLabelRequest>
{
    public UpdateWhiteLabelRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.SupportEmail).MaximumLength(256);
        RuleFor(x => x.SupportPhone).MaximumLength(40);
        RuleFor(x => x.PrimaryColor).MaximumLength(7);
        RuleFor(x => x.LogoUrl).MaximumLength(2048);
        RuleFor(x => x.CustomDomain).MaximumLength(253);
    }
}

public sealed class StartAgencyWorkflowRequestValidator : AbstractValidator<StartAgencyWorkflowRequest>
{
    public StartAgencyWorkflowRequestValidator()
    {
        RuleFor(x => x.Kind).NotEmpty().MaximumLength(32);
    }
}

public sealed class RecordAgencyDecisionRequestValidator : AbstractValidator<RecordAgencyDecisionRequest>
{
    public RecordAgencyDecisionRequestValidator()
    {
        RuleFor(x => x.Decision).NotEmpty().MaximumLength(500);
    }
}

public sealed class StartDisasterDrillRequestValidator : AbstractValidator<StartDisasterDrillRequest>
{
    public StartDisasterDrillRequestValidator()
    {
        RuleFor(x => x.Kind).NotEmpty().MaximumLength(16);
    }
}

public sealed class CreateHubContentRequestValidator : AbstractValidator<CreateHubContentRequest>
{
    public CreateHubContentRequestValidator()
    {
        RuleFor(x => x.ContentTypeCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Body).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Excerpt).MaximumLength(500);
        RuleFor(x => x.Visibility).NotEmpty();
        RuleFor(x => x.CanonicalUrl).MaximumLength(2048);
    }
}

public sealed class UpdateHubContentRequestValidator : AbstractValidator<UpdateHubContentRequest>
{
    public UpdateHubContentRequestValidator()
    {
        RuleFor(x => x.ContentTypeCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Body).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Excerpt).MaximumLength(500);
        RuleFor(x => x.Visibility).NotEmpty();
        RuleFor(x => x.ChangeSummary).MaximumLength(500);
        RuleFor(x => x.CanonicalUrl).MaximumLength(2048);
    }
}

public sealed class ScheduleHubContentRequestValidator : AbstractValidator<ScheduleHubContentRequest>
{
    public ScheduleHubContentRequestValidator()
    {
        RuleFor(x => x.ScheduledAtUtc).Must(at => at > DateTimeOffset.UtcNow).WithMessage("Schedule a time in the future.");
        RuleFor(x => x.Channel).MaximumLength(32);
    }
}

public sealed class GenerateHubContentRequestValidator : AbstractValidator<GenerateHubContentRequest>
{
    public GenerateHubContentRequestValidator()
    {
        RuleFor(x => x.Prompt).NotEmpty().MinimumLength(4).MaximumLength(4000);
    }
}

public sealed class DistributeHubContentRequestValidator : AbstractValidator<DistributeHubContentRequest>
{
    public DistributeHubContentRequestValidator()
    {
        RuleFor(x => x.ProviderCode).NotEmpty().MaximumLength(32);
    }
}

public sealed class RejectHubContentRequestValidator : AbstractValidator<RejectHubContentRequest>
{
    public RejectHubContentRequestValidator()
    {
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public sealed class AttachHubMediaRequestValidator : AbstractValidator<AttachHubMediaRequest>
{
    public AttachHubMediaRequestValidator()
    {
        RuleFor(x => x.MediaAssetId).NotEmpty();
        RuleFor(x => x.Role).NotEmpty().MaximumLength(24);
    }
}

public sealed class RegisterHubMediaRequestValidator : AbstractValidator<RegisterHubMediaRequest>
{
    public RegisterHubMediaRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Kind).NotEmpty().MaximumLength(24);
        RuleFor(x => x.SourceUrl).NotEmpty().MaximumLength(2048);
    }
}
