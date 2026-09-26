using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Ai;
using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Connections;
using DigitalPulse.Application.Features.Identity;
using DigitalPulse.Contracts.WhatsApp;
using DigitalPulse.Domain.Ai;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Platforms;
using DigitalPulse.Domain.WhatsApp;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.WhatsApp;

internal static class WhatsAppMaps
{
    public static WhatsAppMessageKind ParseKind(string value) =>
        Enum.TryParse<WhatsAppMessageKind>(value, true, out var parsed) && parsed != WhatsAppMessageKind.Inbound
            ? parsed
            : throw AppException.Validation("Kind must be Template or Session.");

    public static WhatsAppAccountResponse ToResponse(this WhatsAppAccount account, IWhatsAppCloudApi cloud) =>
        new(
            account.Id,
            account.DisplayName,
            account.PhoneNumber,
            account.Status.ToString(),
            account.PhoneStatus.ToString(),
            account.ConnectionId is not null,
            account.PhoneStatus == WhatsAppPhoneStatus.Verified,
            account.HoldReason,
            account.WabaId,
            account.LastHealthAtUtc,
            account.LastHealthDetail,
            cloud.IsLive,
            cloud.ProviderName);

    public static WhatsAppContactResponse ToResponse(this WhatsAppContact contact) =>
        new(
            contact.Id,
            contact.DisplayName,
            contact.Mobile,
            contact.Consent.ToString(),
            contact.OptedInAtUtc,
            contact.OptedOutAtUtc,
            contact.LastInboundAtUtc,
            WhatsAppPolicy.WindowOpen(contact.LastInboundAtUtc),
            contact.CustomerId);

    public static WhatsAppTemplateResponse ToResponse(this WhatsAppTemplate template) =>
        new(template.Id, template.Name, template.Language, template.Category, template.Body, template.Status.ToString(), template.HoldReason);

    public static WhatsAppCampaignResponse ToResponse(this WhatsAppCampaign campaign) =>
        new(
            campaign.Id,
            campaign.TemplateId,
            campaign.Name,
            campaign.Status.ToString(),
            campaign.ScheduledAtUtc,
            campaign.HoldReason,
            campaign.AudienceCount,
            campaign.SendCount,
            campaign.HeldCount,
            campaign.FailedCount);

    public static WhatsAppMessageResponse ToResponse(this WhatsAppMessage message, IReadOnlyList<WhatsAppMessageAttempt> attempts) =>
        new(
            message.Id,
            message.ContactId,
            message.ConversationId,
            message.CampaignId,
            message.TemplateId,
            message.Kind.ToString(),
            message.Status.ToString(),
            message.Body,
            message.HoldReason,
            message.Untrusted,
            message.ProviderMessageId,
            message.CreatedAtUtc,
            attempts.Select(a => new WhatsAppAttemptResponse(a.Ordinal, a.Outcome, a.Detail, a.CreatedAtUtc)).ToList());
}

internal static class WhatsAppStore
{
    public static async Task<(SubscriptionPlan Plan, int Used)> PlanAsync(IAppDbContext db, Guid tenantId, CancellationToken cancellationToken)
    {
        var subscription = await db.Subscriptions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken)
            ?? throw AppException.Validation("Complete plan selection before opening WhatsApp.");
        var plan = await db.Plans.AsNoTracking().FirstAsync(p => p.Id == subscription.PlanId, cancellationToken);
        var start = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var used = await db.WhatsAppMessages.CountAsync(
            m => m.TenantId == tenantId && m.Kind != WhatsAppMessageKind.Inbound && m.CreatedAtUtc >= start, cancellationToken);
        return (plan, used);
    }

    public static async Task<WhatsAppWorkspaceResponse> LoadAsync(
        IAppDbContext db,
        Guid businessId,
        SubscriptionPlan plan,
        int used,
        IWhatsAppCloudApi cloud,
        CancellationToken cancellationToken)
    {
        var account = await db.WhatsAppAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.BusinessId == businessId, cancellationToken);
        var contacts = await db.WhatsAppContacts.AsNoTracking().Where(c => c.BusinessId == businessId).OrderBy(c => c.DisplayName).ToListAsync(cancellationToken);
        var templates = await db.WhatsAppTemplates.AsNoTracking().Where(t => t.BusinessId == businessId).OrderByDescending(t => t.UpdatedAtUtc).ToListAsync(cancellationToken);
        var campaigns = await db.WhatsAppCampaigns.AsNoTracking().Where(c => c.BusinessId == businessId).OrderByDescending(c => c.UpdatedAtUtc).Take(20).ToListAsync(cancellationToken);
        var conversations = await db.WhatsAppConversations.AsNoTracking().Where(c => c.BusinessId == businessId).OrderByDescending(c => c.UpdatedAtUtc).Take(20).ToListAsync(cancellationToken);
        var messages = await db.WhatsAppMessages.AsNoTracking().Where(m => m.BusinessId == businessId).OrderByDescending(m => m.UpdatedAtUtc).Take(40).ToListAsync(cancellationToken);
        var attempts = await db.WhatsAppMessageAttempts.AsNoTracking()
            .Where(a => messages.Select(m => m.Id).Contains(a.MessageId))
            .OrderBy(a => a.Ordinal)
            .ToListAsync(cancellationToken);

        var analytics = new WhatsAppAnalyticsResponse(
            contacts.Count(c => c.Consent == WhatsAppConsentStatus.OptedIn),
            contacts.Count(c => c.Consent == WhatsAppConsentStatus.OptedOut),
            templates.Count(t => t.Status == WhatsAppTemplateStatus.Approved),
            campaigns.Count,
            messages.Count(m => m.Status == WhatsAppMessageStatus.Held),
            messages.Count(m => m.Status == WhatsAppMessageStatus.Failed),
            messages.Count(m => m.Kind == WhatsAppMessageKind.Inbound && m.Untrusted),
            "Delivery, read, and campaign reach come from Cloud API webhooks. Those counts are not invented.");

        return new WhatsAppWorkspaceResponse(
            plan.WhatsAppEnabled,
            plan.WhatsAppMessagesPerMonth,
            used,
            "WhatsApp Business Messaging uses Cloud API only. A mobile number is not enough to send. Opt-in, an approved template or an open 24-hour window, and a live Cloud API are required. Unofficial clients are out of scope.",
            account?.ToResponse(cloud),
            analytics,
            contacts.Select(c => c.ToResponse()).ToList(),
            templates.Select(t => t.ToResponse()).ToList(),
            campaigns.Select(c => c.ToResponse()).ToList(),
            conversations.Select(conversation => new WhatsAppConversationResponse(
                conversation.Id,
                conversation.ContactId,
                conversation.LastInboundAtUtc,
                conversation.LastOutboundAtUtc,
                conversation.WindowOpenUntilUtc,
                conversation.IsWindowOpen(),
                messages.Where(m => m.ConversationId == conversation.Id)
                    .OrderBy(m => m.CreatedAtUtc)
                    .Select(m => m.ToResponse(attempts.Where(a => a.MessageId == m.Id).ToList()))
                    .ToList())).ToList(),
            messages.Select(m => m.ToResponse(attempts.Where(a => a.MessageId == m.Id).ToList())).ToList());
    }

    public static async Task<WhatsAppMessageResponse> LoadMessageAsync(IAppDbContext db, Guid messageId, CancellationToken cancellationToken)
    {
        var message = await db.WhatsAppMessages.AsNoTracking().FirstAsync(m => m.Id == messageId, cancellationToken);
        var attempts = await db.WhatsAppMessageAttempts.AsNoTracking()
            .Where(a => a.MessageId == messageId)
            .OrderBy(a => a.Ordinal)
            .ToListAsync(cancellationToken);
        return message.ToResponse(attempts);
    }
}

public sealed class GetWhatsAppWorkspaceHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IWhatsAppCloudApi _cloud;

    public GetWhatsAppWorkspaceHandler(IAppDbContext db, ITenantContext tenant, IWhatsAppCloudApi cloud)
    {
        _db = db;
        _tenant = tenant;
        _cloud = cloud;
    }

    public async Task<WhatsAppWorkspaceResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var (plan, used) = await WhatsAppStore.PlanAsync(_db, tenantId, cancellationToken);
        return await WhatsAppStore.LoadAsync(_db, businessId, plan, used, _cloud, cancellationToken);
    }
}

public sealed class ConnectWhatsAppHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IWhatsAppCloudApi _cloud;
    private readonly StartConnectionHandler _start;

    public ConnectWhatsAppHandler(
        IAppDbContext db,
        ITenantContext tenant,
        IWhatsAppCloudApi cloud,
        StartConnectionHandler start)
    {
        _db = db;
        _tenant = tenant;
        _cloud = cloud;
        _start = start;
    }

    public async Task<WhatsAppWorkspaceResponse> Handle(Guid businessId, ConnectWhatsAppRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var business = await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var (plan, used) = await WhatsAppStore.PlanAsync(_db, tenantId, cancellationToken);
        try
        {
            EntitlementRules.EnsureCanUseWhatsApp(plan);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            throw AppException.Validation("Give the WhatsApp Business display name and phone number.");
        }

        var connection = await _db.Connections.FirstOrDefaultAsync(
            c => c.BusinessId == businessId && c.PlatformCode == "WHATSAPP", cancellationToken);
        if (connection is null || connection.Status != ConnectionStatus.Connected)
        {
            var started = await _start.Handle(businessId, "WHATSAPP", cancellationToken);
            connection = await _db.Connections.FirstAsync(c => c.Id == started.Connection.Id, cancellationToken);
        }

        var account = await _db.WhatsAppAccounts.FirstOrDefaultAsync(a => a.BusinessId == businessId, cancellationToken);
        if (account is null)
        {
            account = WhatsAppAccount.Attach(tenantId, businessId, connection.Id, request.DisplayName, request.PhoneNumber);
            _db.WhatsAppAccounts.Add(account);
        }

        account.MarkConnected(connection.Id, null);
        var health = await _cloud.HealthAsync(cancellationToken);
        account.RecordHealth(health.Detail, !health.IsLive);
        await _db.SaveChangesAsync(cancellationToken);
        _ = business;
        return await WhatsAppStore.LoadAsync(_db, businessId, plan, used, _cloud, cancellationToken);
    }
}

public sealed class VerifyWhatsAppPhoneHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IWhatsAppCloudApi _cloud;

    public VerifyWhatsAppPhoneHandler(IAppDbContext db, ITenantContext tenant, IWhatsAppCloudApi cloud)
    {
        _db = db;
        _tenant = tenant;
        _cloud = cloud;
    }

    public async Task<WhatsAppWorkspaceResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var account = await RequireAccountAsync(_db, businessId, cancellationToken);
        var result = await _cloud.VerifyPhoneAsync(account.PhoneNumber, cancellationToken);
        account.MarkPhone(
            result.IsLive && result.Status == "Accepted" ? WhatsAppPhoneStatus.Verified : WhatsAppPhoneStatus.Held,
            result.Detail,
            result.IsLive && result.Status == "Accepted");
        await _db.SaveChangesAsync(cancellationToken);
        var (plan, used) = await WhatsAppStore.PlanAsync(_db, tenantId, cancellationToken);
        return await WhatsAppStore.LoadAsync(_db, businessId, plan, used, _cloud, cancellationToken);
    }

    internal static async Task<WhatsAppAccount> RequireAccountAsync(IAppDbContext db, Guid businessId, CancellationToken cancellationToken) =>
        await db.WhatsAppAccounts.FirstOrDefaultAsync(a => a.BusinessId == businessId, cancellationToken)
        ?? throw AppException.Validation("Connect WhatsApp Business before continuing.");
}

public sealed class ImportWhatsAppContactsHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IWhatsAppCloudApi _cloud;

    public ImportWhatsAppContactsHandler(IAppDbContext db, ITenantContext tenant, IWhatsAppCloudApi cloud)
    {
        _db = db;
        _tenant = tenant;
        _cloud = cloud;
    }

    public async Task<WhatsAppWorkspaceResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var (plan, used) = await WhatsAppStore.PlanAsync(_db, tenantId, cancellationToken);
        try
        {
            EntitlementRules.EnsureCanUseWhatsApp(plan);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        var customers = await _db.Customers.Where(c => c.BusinessId == businessId).ToListAsync(cancellationToken);
        var mobiles = await _db.CustomerContacts
            .Where(c => c.BusinessId == businessId && c.Kind == CustomerContactKind.Mobile)
            .ToListAsync(cancellationToken);
        var existing = await _db.WhatsAppContacts.Where(c => c.BusinessId == businessId).Select(c => c.Mobile).ToListAsync(cancellationToken);

        foreach (var mobile in mobiles)
        {
            if (existing.Contains(mobile.Value, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var customer = customers.FirstOrDefault(c => c.Id == mobile.CustomerId);
            _db.WhatsAppContacts.Add(WhatsAppContact.Import(
                tenantId,
                businessId,
                mobile.CustomerId,
                mobile.Id,
                customer?.DisplayName ?? mobile.Value,
                mobile.Value));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await WhatsAppStore.LoadAsync(_db, businessId, plan, used, _cloud, cancellationToken);
    }
}

public sealed class SetWhatsAppConsentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IWhatsAppCloudApi _cloud;

    public SetWhatsAppConsentHandler(IAppDbContext db, ITenantContext tenant, IWhatsAppCloudApi cloud)
    {
        _db = db;
        _tenant = tenant;
        _cloud = cloud;
    }

    public async Task<WhatsAppWorkspaceResponse> Handle(Guid businessId, Guid contactId, bool optIn, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var contact = await _db.WhatsAppContacts.FirstOrDefaultAsync(c => c.Id == contactId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("WhatsApp contact was not found.");
        if (optIn) contact.OptIn();
        else contact.OptOut();
        await _db.SaveChangesAsync(cancellationToken);
        var (plan, used) = await WhatsAppStore.PlanAsync(_db, tenantId, cancellationToken);
        return await WhatsAppStore.LoadAsync(_db, businessId, plan, used, _cloud, cancellationToken);
    }
}

public sealed class CreateWhatsAppTemplateHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public CreateWhatsAppTemplateHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<WhatsAppTemplateResponse> Handle(Guid businessId, CreateWhatsAppTemplateRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Body))
        {
            throw AppException.Validation("Template name and body are required.");
        }

        ContentGuard.Require(request.Name, request.Body);

        if (await _db.WhatsAppTemplates.AnyAsync(t => t.BusinessId == businessId && t.Name == request.Name.Trim(), cancellationToken))
        {
            throw AppException.Conflict("A template with that name already exists.");
        }

        var template = WhatsAppTemplate.Draft(tenantId, businessId, request.Name, request.Language, request.Category, request.Body);
        _db.WhatsAppTemplates.Add(template);
        await _db.SaveChangesAsync(cancellationToken);
        return template.ToResponse();
    }
}

public sealed class ApproveWhatsAppTemplateHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public ApproveWhatsAppTemplateHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<WhatsAppTemplateResponse> Handle(Guid businessId, Guid templateId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var template = await _db.WhatsAppTemplates.FirstOrDefaultAsync(t => t.Id == templateId && t.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Template was not found.");
        template.Approve();
        await _db.SaveChangesAsync(cancellationToken);
        return template.ToResponse();
    }
}

public sealed class CreateWhatsAppCampaignHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public CreateWhatsAppCampaignHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<WhatsAppCampaignResponse> Handle(Guid businessId, CreateWhatsAppCampaignRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var template = await _db.WhatsAppTemplates.FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Template was not found.");
        if (template.Status != WhatsAppTemplateStatus.Approved)
        {
            throw AppException.Validation("Approve the template before creating a campaign.");
        }

        var campaign = WhatsAppCampaign.Draft(tenantId, businessId, template.Id, request.Name);
        _db.WhatsAppCampaigns.Add(campaign);
        await _db.SaveChangesAsync(cancellationToken);
        return campaign.ToResponse();
    }
}

public sealed class ApproveWhatsAppCampaignHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public ApproveWhatsAppCampaignHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<WhatsAppCampaignResponse> Handle(Guid businessId, Guid campaignId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var campaign = await _db.WhatsAppCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Campaign was not found.");
        var optedIn = await _db.WhatsAppContacts.CountAsync(c => c.BusinessId == businessId && c.Consent == WhatsAppConsentStatus.OptedIn, cancellationToken);
        campaign.Approve(optedIn);
        await _db.SaveChangesAsync(cancellationToken);
        return campaign.ToResponse();
    }
}

public sealed class ScheduleWhatsAppCampaignHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly SendWhatsAppMessageHandler _send;

    public ScheduleWhatsAppCampaignHandler(IAppDbContext db, ITenantContext tenant, SendWhatsAppMessageHandler send)
    {
        _db = db;
        _tenant = tenant;
        _send = send;
    }

    public async Task<WhatsAppCampaignResponse> Handle(Guid businessId, Guid campaignId, ScheduleWhatsAppCampaignRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var campaign = await _db.WhatsAppCampaigns.FirstOrDefaultAsync(c => c.Id == campaignId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Campaign was not found.");
        var template = await _db.WhatsAppTemplates.FirstAsync(t => t.Id == campaign.TemplateId, cancellationToken);
        try
        {
            campaign.Schedule(request.ScheduledAtUtc);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        var contacts = await _db.WhatsAppContacts.Where(c => c.BusinessId == businessId && c.Consent == WhatsAppConsentStatus.OptedIn).ToListAsync(cancellationToken);
        var sent = 0;
        var held = 0;
        var failed = 0;
        foreach (var contact in contacts)
        {
            var message = WhatsAppMessage.Draft(tenantId, businessId, contact.Id, WhatsAppMessageKind.Template, template.Body, null, campaign.Id, template.Id);
            message.Approve();
            _db.WhatsAppMessages.Add(message);
            await _db.SaveChangesAsync(cancellationToken);
            var result = await _send.Handle(businessId, message.Id, cancellationToken);
            if (result.Status == "Held") held++;
            else if (result.Status == "Failed") failed++;
            else sent++;
        }

        campaign.RecordResult(sent, held, failed, "Campaign recipients were evaluated. Live Cloud API delivery is not invented.");
        await _db.SaveChangesAsync(cancellationToken);
        return campaign.ToResponse();
    }
}

public sealed class DraftWhatsAppMessageHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public DraftWhatsAppMessageHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<WhatsAppMessageResponse> Handle(Guid businessId, DraftWhatsAppMessageRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var kind = WhatsAppMaps.ParseKind(request.Kind);
        var contact = await _db.WhatsAppContacts.FirstOrDefaultAsync(c => c.Id == request.ContactId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("WhatsApp contact was not found.");
        if (string.IsNullOrWhiteSpace(request.Body) || request.Body.Trim().Length < 3)
        {
            throw AppException.Validation("Write a short message body.");
        }

        ContentGuard.Require(request.Body);

        if (kind == WhatsAppMessageKind.Template)
        {
            var template = await _db.WhatsAppTemplates.FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.BusinessId == businessId, cancellationToken)
                ?? throw AppException.Validation("Choose an approved template for a business-initiated message.");
            _ = template;
        }

        var conversation = await _db.WhatsAppConversations.FirstOrDefaultAsync(c => c.BusinessId == businessId && c.ContactId == contact.Id, cancellationToken);
        var message = WhatsAppMessage.Draft(tenantId, businessId, contact.Id, kind, request.Body, conversation?.Id, null, request.TemplateId);
        _db.WhatsAppMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);
        return await WhatsAppStore.LoadMessageAsync(_db, message.Id, cancellationToken);
    }
}

public sealed class DraftWhatsAppAiHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAiOrchestrator _ai;

    public DraftWhatsAppAiHandler(IAppDbContext db, ITenantContext tenant, IAiOrchestrator ai)
    {
        _db = db;
        _tenant = tenant;
        _ai = ai;
    }

    public async Task<WhatsAppMessageResponse> Handle(Guid businessId, DraftWhatsAppAiRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var business = await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var kind = WhatsAppMaps.ParseKind(request.Kind);
        var contact = await _db.WhatsAppContacts.FirstOrDefaultAsync(c => c.Id == request.ContactId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("WhatsApp contact was not found.");
        if (string.IsNullOrWhiteSpace(request.Prompt) || request.Prompt.Trim().Length < 4)
        {
            throw AppException.Validation("Give the orchestrator a short prompt for the draft.");
        }

        var evidence = new List<AiEvidence>
        {
            new("business", "NAME", business.Name, false, true),
            new("contact", "NAME", contact.DisplayName, false, true)
        };
        var completion = await _ai.RunAsync(
            new AiOrchestrationRequest("whatsapp", request.Prompt.Trim(), evidence, [$"Business:{business.Name}", $"Contact:{contact.DisplayName}"]),
            cancellationToken);
        var message = WhatsAppMessage.Draft(
            tenantId,
            businessId,
            contact.Id,
            kind,
            completion.Output,
            null,
            null,
            request.TemplateId);
        if (!completion.ProviderIsLive)
        {
            message.MarkHeld("AI draft assembled without a live model. It was not sent.");
        }

        _db.WhatsAppMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);
        return await WhatsAppStore.LoadMessageAsync(_db, message.Id, cancellationToken);
    }
}

public sealed class ApproveWhatsAppMessageHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public ApproveWhatsAppMessageHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<WhatsAppMessageResponse> Handle(Guid businessId, Guid messageId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var message = await _db.WhatsAppMessages.FirstOrDefaultAsync(m => m.Id == messageId && m.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Message was not found.");
        try
        {
            message.Approve();
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await WhatsAppStore.LoadMessageAsync(_db, message.Id, cancellationToken);
    }
}

public sealed class SendWhatsAppMessageHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IWhatsAppCloudApi _cloud;

    public SendWhatsAppMessageHandler(IAppDbContext db, ITenantContext tenant, IWhatsAppCloudApi cloud)
    {
        _db = db;
        _tenant = tenant;
        _cloud = cloud;
    }

    public async Task<WhatsAppMessageResponse> Handle(Guid businessId, Guid messageId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var (plan, used) = await WhatsAppStore.PlanAsync(_db, tenantId, cancellationToken);
        try
        {
            EntitlementRules.EnsureCanSendWhatsApp(plan, used);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        var message = await _db.WhatsAppMessages.FirstOrDefaultAsync(m => m.Id == messageId && m.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Message was not found.");
        if (message.Status is not (WhatsAppMessageStatus.Approved or WhatsAppMessageStatus.Queued or WhatsAppMessageStatus.Held or WhatsAppMessageStatus.Failed))
        {
            throw AppException.Validation("Approve the message before sending.");
        }

        var account = await _db.WhatsAppAccounts.FirstOrDefaultAsync(a => a.BusinessId == businessId, cancellationToken);
        var contact = await _db.WhatsAppContacts.FirstAsync(c => c.Id == message.ContactId, cancellationToken);
        var template = message.TemplateId is null
            ? null
            : await _db.WhatsAppTemplates.FirstOrDefaultAsync(t => t.Id == message.TemplateId, cancellationToken);
        var conversation = await _db.WhatsAppConversations.FirstOrDefaultAsync(
            c => c.BusinessId == businessId && c.ContactId == contact.Id, cancellationToken);
        var windowOpen = conversation?.IsWindowOpen() == true || WhatsAppPolicy.WindowOpen(contact.LastInboundAtUtc);
        var decision = WhatsAppPolicy.Evaluate(
            plan.WhatsAppEnabled,
            account is { ConnectionId: not null },
            account?.PhoneStatus == WhatsAppPhoneStatus.Verified,
            contact.Consent,
            message.Kind,
            template?.Status == WhatsAppTemplateStatus.Approved,
            windowOpen,
            _cloud.IsLive);

        if (!decision.Allowed)
        {
            message.MarkFailed(decision.Reason);
            _db.WhatsAppMessageAttempts.Add(WhatsAppMessageAttempt.Record(tenantId, message.Id, await NextOrdinalAsync(message.Id, cancellationToken), "Blocked", decision.Reason));
            await _db.SaveChangesAsync(cancellationToken);
            return await WhatsAppStore.LoadMessageAsync(_db, message.Id, cancellationToken);
        }

        var result = message.Kind == WhatsAppMessageKind.Template
            ? await _cloud.SendTemplateAsync(contact.Mobile, template?.Name ?? "unnamed", message.Body, cancellationToken)
            : await _cloud.SendSessionAsync(contact.Mobile, message.Body, cancellationToken);

        if (result.Status == "Accepted" && result.IsLive && result.ProviderMessageId is not null)
        {
            message.MarkDelivered(result.ProviderMessageId);
            if (conversation is null)
            {
                conversation = WhatsAppConversation.Open(tenantId, businessId, contact.Id);
                _db.WhatsAppConversations.Add(conversation);
            }

            conversation.RecordOutbound(DateTimeOffset.UtcNow);
            _db.WhatsAppMessageAttempts.Add(WhatsAppMessageAttempt.Record(tenantId, message.Id, await NextOrdinalAsync(message.Id, cancellationToken), "Delivered", result.Detail));
        }
        else if (result.Status == "Failed")
        {
            message.MarkFailed(result.Detail);
            _db.WhatsAppMessageAttempts.Add(WhatsAppMessageAttempt.Record(tenantId, message.Id, await NextOrdinalAsync(message.Id, cancellationToken), "Failed", result.Detail));
        }
        else
        {
            message.MarkHeld(decision.Reason);
            _db.WhatsAppMessageAttempts.Add(WhatsAppMessageAttempt.Record(tenantId, message.Id, await NextOrdinalAsync(message.Id, cancellationToken), "Held", result.Detail));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await WhatsAppStore.LoadMessageAsync(_db, message.Id, cancellationToken);
    }

    private Task<int> NextOrdinalAsync(Guid messageId, CancellationToken cancellationToken) =>
        _db.WhatsAppMessageAttempts.CountAsync(a => a.MessageId == messageId, cancellationToken).ContinueWith(t => t.Result + 1, cancellationToken);
}

public sealed class RecordWhatsAppInboundHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IWhatsAppCloudApi _cloud;

    public RecordWhatsAppInboundHandler(IAppDbContext db, ITenantContext tenant, IWhatsAppCloudApi cloud)
    {
        _db = db;
        _tenant = tenant;
        _cloud = cloud;
    }

    public async Task<WhatsAppWorkspaceResponse> Handle(Guid businessId, RecordWhatsAppInboundRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var contact = await _db.WhatsAppContacts.FirstOrDefaultAsync(c => c.Id == request.ContactId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("WhatsApp contact was not found.");
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            throw AppException.Validation("Inbound body is required.");
        }

        await RecordInboundAsync(_db, tenantId, businessId, contact, request.Body, trusted: false, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        var (plan, used) = await WhatsAppStore.PlanAsync(_db, tenantId, cancellationToken);
        return await WhatsAppStore.LoadAsync(_db, businessId, plan, used, _cloud, cancellationToken);
    }

    internal static async Task RecordInboundAsync(
        IAppDbContext db,
        Guid tenantId,
        Guid businessId,
        WhatsAppContact contact,
        string body,
        bool trusted,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        contact.RecordInbound(now);
        var conversation = await db.WhatsAppConversations.FirstOrDefaultAsync(c => c.BusinessId == businessId && c.ContactId == contact.Id, cancellationToken);
        if (conversation is null)
        {
            conversation = WhatsAppConversation.Open(tenantId, businessId, contact.Id);
            db.WhatsAppConversations.Add(conversation);
        }

        conversation.RecordInbound(now);
        db.WhatsAppMessages.Add(WhatsAppMessage.Inbound(tenantId, businessId, contact.Id, conversation.Id, body, trusted));
        db.WhatsAppWebhookEvents.Add(WhatsAppWebhookEvent.Record(
            tenantId,
            businessId,
            trusted ? "webhook-inbound" : "operator-inbound",
            trusted ? "Verified Cloud API webhook." : "Operator-recorded inbound. Untrusted until a signed Cloud API webhook exists.",
            trusted));
    }
}

public sealed class ReplyWhatsAppHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly DraftWhatsAppMessageHandler _draft;
    private readonly ApproveWhatsAppMessageHandler _approve;
    private readonly SendWhatsAppMessageHandler _send;

    public ReplyWhatsAppHandler(
        IAppDbContext db,
        ITenantContext tenant,
        DraftWhatsAppMessageHandler draft,
        ApproveWhatsAppMessageHandler approve,
        SendWhatsAppMessageHandler send)
    {
        _db = db;
        _tenant = tenant;
        _draft = draft;
        _approve = approve;
        _send = send;
    }

    public async Task<WhatsAppMessageResponse> Handle(Guid businessId, Guid conversationId, ReplyWhatsAppRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var conversation = await _db.WhatsAppConversations.FirstOrDefaultAsync(c => c.Id == conversationId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Conversation was not found.");
        var drafted = await _draft.Handle(businessId, new DraftWhatsAppMessageRequest(conversation.ContactId, "Session", request.Body, null), cancellationToken);
        await _approve.Handle(businessId, drafted.Id, cancellationToken);
        return await _send.Handle(businessId, drafted.Id, cancellationToken);
    }
}
