using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.WhatsApp;

public enum WhatsAppAccountStatus
{
    Draft = 0,
    Connected = 1,
    NeedsVerify = 2,
    Held = 3
}

public enum WhatsAppPhoneStatus
{
    Unverified = 0,
    Pending = 1,
    Verified = 2,
    Held = 3
}

public enum WhatsAppConsentStatus
{
    Unknown = 0,
    OptedIn = 1,
    OptedOut = 2
}

public enum WhatsAppTemplateStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3
}

public enum WhatsAppCampaignStatus
{
    Draft = 0,
    Approved = 1,
    Scheduled = 2,
    Held = 3,
    Failed = 4
}

public enum WhatsAppMessageKind
{
    Template = 0,
    Session = 1,
    Inbound = 2
}

public enum WhatsAppMessageStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Queued = 3,
    Held = 4,
    Failed = 5,
    Delivered = 6,
    Read = 7
}

public sealed record WhatsAppSendDecision(bool Allowed, bool CanSendLive, string Reason);

public static class WhatsAppPolicy
{
    public static readonly TimeSpan CustomerServiceWindow = TimeSpan.FromHours(24);

    public static bool WindowOpen(DateTimeOffset? lastInboundAtUtc, DateTimeOffset? now = null)
    {
        var at = now ?? DateTimeOffset.UtcNow;
        return lastInboundAtUtc is not null && at - lastInboundAtUtc.Value <= CustomerServiceWindow;
    }

    public static WhatsAppSendDecision Evaluate(
        bool planEnabled,
        bool connected,
        bool phoneVerified,
        WhatsAppConsentStatus consent,
        WhatsAppMessageKind kind,
        bool templateApproved,
        bool windowOpen,
        bool liveCloudApi)
    {
        if (!planEnabled)
        {
            return new(false, false, "This plan does not include WhatsApp Business Messaging.");
        }

        if (consent == WhatsAppConsentStatus.OptedOut)
        {
            return new(false, false, "Opt-out stops campaign and template sends immediately.");
        }

        if (consent != WhatsAppConsentStatus.OptedIn)
        {
            return new(false, false, "A stored mobile number without an explicit WhatsApp opt-in is not a sendable destination.");
        }

        if (!connected)
        {
            return new(false, false, "Connect a WhatsApp Business account through Cloud API before sending.");
        }

        if (!phoneVerified && liveCloudApi)
        {
            return new(false, false, "Verify the business phone number before sending.");
        }

        if (kind == WhatsAppMessageKind.Template && !templateApproved)
        {
            return new(false, false, "Business-initiated conversations require an approved WhatsApp message template.");
        }

        if (kind == WhatsAppMessageKind.Session && !windowOpen)
        {
            return new(false, false, "Session replies require an open 24-hour customer-service window.");
        }

        if (kind == WhatsAppMessageKind.Inbound)
        {
            return new(false, false, "Inbound messages are received, not sent.");
        }

        if (!liveCloudApi)
        {
            return new(true, false, "Policy passed. A live WhatsApp Cloud API is not configured, so the send stays held. Unofficial clients are out of scope.");
        }

        return new(true, true, "Policy passed. Official Cloud API may send this message.");
    }
}

public sealed class WhatsAppAccount : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public Guid? ConnectionId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string? WabaId { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public WhatsAppAccountStatus Status { get; private set; } = WhatsAppAccountStatus.Draft;
    public WhatsAppPhoneStatus PhoneStatus { get; private set; } = WhatsAppPhoneStatus.Unverified;
    public string HoldReason { get; private set; } = string.Empty;
    public DateTimeOffset? PhoneVerifiedAtUtc { get; private set; }
    public DateTimeOffset? LastHealthAtUtc { get; private set; }
    public string? LastHealthDetail { get; private set; }

    private WhatsAppAccount() { }

    public static WhatsAppAccount Attach(
        Guid tenantId,
        Guid businessId,
        Guid? connectionId,
        string displayName,
        string phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        return new WhatsAppAccount
        {
            TenantId = tenantId,
            BusinessId = businessId,
            ConnectionId = connectionId,
            DisplayName = displayName.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            Status = WhatsAppAccountStatus.NeedsVerify,
            PhoneStatus = WhatsAppPhoneStatus.Unverified,
            HoldReason = "Cloud API grant recorded. Business phone verification is still required."
        };
    }

    public void MarkConnected(Guid connectionId, string? wabaId)
    {
        ConnectionId = connectionId;
        WabaId = string.IsNullOrWhiteSpace(wabaId) ? null : wabaId.Trim();
        Status = WhatsAppAccountStatus.NeedsVerify;
        HoldReason = "WhatsApp Business account is connected. Verify the business phone before sending.";
        Touch();
    }

    public void RecordHealth(string detail, bool held)
    {
        LastHealthAtUtc = DateTimeOffset.UtcNow;
        LastHealthDetail = detail.Trim();
        if (held && Status == WhatsAppAccountStatus.Connected)
        {
            Status = WhatsAppAccountStatus.Held;
        }

        HoldReason = detail.Trim();
        Touch();
    }

    public void MarkPhone(WhatsAppPhoneStatus status, string detail, bool liveVerified)
    {
        PhoneStatus = liveVerified ? WhatsAppPhoneStatus.Verified : status;
        PhoneVerifiedAtUtc = liveVerified ? DateTimeOffset.UtcNow : PhoneVerifiedAtUtc;
        if (liveVerified)
        {
            Status = WhatsAppAccountStatus.Connected;
        }
        else if (status == WhatsAppPhoneStatus.Held)
        {
            Status = WhatsAppAccountStatus.Held;
        }

        HoldReason = detail.Trim();
        Touch();
    }

    public bool IsReady =>
        Status is WhatsAppAccountStatus.Connected or WhatsAppAccountStatus.Held &&
        PhoneStatus == WhatsAppPhoneStatus.Verified;
}

public sealed class WhatsAppContact : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? CustomerContactId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string Mobile { get; private set; } = string.Empty;
    public WhatsAppConsentStatus Consent { get; private set; } = WhatsAppConsentStatus.Unknown;
    public DateTimeOffset? OptedInAtUtc { get; private set; }
    public DateTimeOffset? OptedOutAtUtc { get; private set; }
    public DateTimeOffset? LastInboundAtUtc { get; private set; }

    private WhatsAppContact() { }

    public static WhatsAppContact Import(
        Guid tenantId,
        Guid businessId,
        Guid? customerId,
        Guid? customerContactId,
        string displayName,
        string mobile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(mobile);
        return new WhatsAppContact
        {
            TenantId = tenantId,
            BusinessId = businessId,
            CustomerId = customerId,
            CustomerContactId = customerContactId,
            DisplayName = displayName.Trim(),
            Mobile = mobile.Trim(),
            Consent = WhatsAppConsentStatus.Unknown
        };
    }

    public void OptIn()
    {
        Consent = WhatsAppConsentStatus.OptedIn;
        OptedInAtUtc = DateTimeOffset.UtcNow;
        OptedOutAtUtc = null;
        Touch();
    }

    public void OptOut()
    {
        Consent = WhatsAppConsentStatus.OptedOut;
        OptedOutAtUtc = DateTimeOffset.UtcNow;
        Touch();
    }

    public void RecordInbound(DateTimeOffset atUtc)
    {
        LastInboundAtUtc = atUtc;
        Touch();
    }
}

public sealed class WhatsAppTemplate : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Language { get; private set; } = "en";
    public string Category { get; private set; } = "UTILITY";
    public string Body { get; private set; } = string.Empty;
    public WhatsAppTemplateStatus Status { get; private set; } = WhatsAppTemplateStatus.Draft;
    public string HoldReason { get; private set; } = string.Empty;

    private WhatsAppTemplate() { }

    public static WhatsAppTemplate Draft(Guid tenantId, Guid businessId, string name, string language, string category, string body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        return new WhatsAppTemplate
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Name = name.Trim(),
            Language = string.IsNullOrWhiteSpace(language) ? "en" : language.Trim(),
            Category = string.IsNullOrWhiteSpace(category) ? "UTILITY" : category.Trim().ToUpperInvariant(),
            Body = body.Trim(),
            Status = WhatsAppTemplateStatus.Draft,
            HoldReason = "Internal draft. Meta template approval is not invented."
        };
    }

    public void Approve()
    {
        Status = WhatsAppTemplateStatus.Approved;
        HoldReason = "Internally approved. Cloud API template status is not invented unless a live provider confirms it.";
        Touch();
    }

    public void Reject(string reason)
    {
        Status = WhatsAppTemplateStatus.Rejected;
        HoldReason = reason.Trim();
        Touch();
    }
}

public sealed class WhatsAppCampaign : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public Guid TemplateId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public WhatsAppCampaignStatus Status { get; private set; } = WhatsAppCampaignStatus.Draft;
    public DateTimeOffset? ScheduledAtUtc { get; private set; }
    public string HoldReason { get; private set; } = string.Empty;
    public int AudienceCount { get; private set; }
    public int SendCount { get; private set; }
    public int HeldCount { get; private set; }
    public int FailedCount { get; private set; }

    private WhatsAppCampaign() { }

    public static WhatsAppCampaign Draft(Guid tenantId, Guid businessId, Guid templateId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new WhatsAppCampaign
        {
            TenantId = tenantId,
            BusinessId = businessId,
            TemplateId = templateId,
            Name = name.Trim(),
            Status = WhatsAppCampaignStatus.Draft,
            HoldReason = "Campaign draft. Approve the template and campaign before queueing sends."
        };
    }

    public void Approve(int audienceCount)
    {
        if (audienceCount < 0) throw new ArgumentOutOfRangeException(nameof(audienceCount));
        AudienceCount = audienceCount;
        Status = WhatsAppCampaignStatus.Approved;
        HoldReason = "Campaign approved. Sends still require opt-in and a live Cloud API.";
        Touch();
    }

    public void Schedule(DateTimeOffset when)
    {
        if (Status != WhatsAppCampaignStatus.Approved)
        {
            throw new InvalidOperationException("Approve the campaign before scheduling.");
        }

        ScheduledAtUtc = when;
        Status = WhatsAppCampaignStatus.Scheduled;
        HoldReason = "Scheduled. Live Cloud API dispatch is not invented.";
        Touch();
    }

    public void RecordResult(int sendCount, int heldCount, int failedCount, string detail)
    {
        SendCount = sendCount;
        HeldCount = heldCount;
        FailedCount = failedCount;
        Status = failedCount > 0 && heldCount == 0 && sendCount == 0 ? WhatsAppCampaignStatus.Failed : WhatsAppCampaignStatus.Held;
        HoldReason = detail.Trim();
        Touch();
    }
}

public sealed class WhatsAppConversation : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public Guid ContactId { get; private set; }
    public DateTimeOffset? LastInboundAtUtc { get; private set; }
    public DateTimeOffset? LastOutboundAtUtc { get; private set; }
    public DateTimeOffset? WindowOpenUntilUtc { get; private set; }

    private WhatsAppConversation() { }

    public static WhatsAppConversation Open(Guid tenantId, Guid businessId, Guid contactId) =>
        new()
        {
            TenantId = tenantId,
            BusinessId = businessId,
            ContactId = contactId
        };

    public void RecordInbound(DateTimeOffset atUtc)
    {
        LastInboundAtUtc = atUtc;
        WindowOpenUntilUtc = atUtc.Add(WhatsAppPolicy.CustomerServiceWindow);
        Touch();
    }

    public void RecordOutbound(DateTimeOffset atUtc)
    {
        LastOutboundAtUtc = atUtc;
        Touch();
    }

    public bool IsWindowOpen(DateTimeOffset? now = null) =>
        WindowOpenUntilUtc is not null && (now ?? DateTimeOffset.UtcNow) <= WindowOpenUntilUtc;
}

public sealed class WhatsAppMessage : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public Guid ContactId { get; private set; }
    public Guid? ConversationId { get; private set; }
    public Guid? CampaignId { get; private set; }
    public Guid? TemplateId { get; private set; }
    public WhatsAppMessageKind Kind { get; private set; }
    public WhatsAppMessageStatus Status { get; private set; } = WhatsAppMessageStatus.Draft;
    public string Body { get; private set; } = string.Empty;
    public string HoldReason { get; private set; } = string.Empty;
    public string? ProviderMessageId { get; private set; }
    public bool Untrusted { get; private set; }

    private WhatsAppMessage() { }

    public static WhatsAppMessage Draft(
        Guid tenantId,
        Guid businessId,
        Guid contactId,
        WhatsAppMessageKind kind,
        string body,
        Guid? conversationId,
        Guid? campaignId,
        Guid? templateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        if (kind == WhatsAppMessageKind.Inbound)
        {
            throw new ArgumentException("Inbound messages are received, not drafted.", nameof(kind));
        }

        return new WhatsAppMessage
        {
            TenantId = tenantId,
            BusinessId = businessId,
            ContactId = contactId,
            ConversationId = conversationId,
            CampaignId = campaignId,
            TemplateId = templateId,
            Kind = kind,
            Body = body.Trim(),
            Status = WhatsAppMessageStatus.Draft,
            HoldReason = "Draft stored. Approval and Cloud API policy still apply."
        };
    }

    public static WhatsAppMessage Inbound(
        Guid tenantId,
        Guid businessId,
        Guid contactId,
        Guid conversationId,
        string body,
        bool trusted)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        return new WhatsAppMessage
        {
            TenantId = tenantId,
            BusinessId = businessId,
            ContactId = contactId,
            ConversationId = conversationId,
            Kind = WhatsAppMessageKind.Inbound,
            Body = body.Trim(),
            Status = WhatsAppMessageStatus.Delivered,
            Untrusted = !trusted,
            HoldReason = trusted
                ? "Inbound Cloud API webhook recorded."
                : "Inbound content is untrusted external content until a verified Cloud API webhook is configured."
        };
    }

    public void Approve()
    {
        if (Status is not (WhatsAppMessageStatus.Draft or WhatsAppMessageStatus.Held or WhatsAppMessageStatus.Failed))
        {
            throw new InvalidOperationException("Only drafts or held messages can be approved.");
        }

        Status = WhatsAppMessageStatus.Approved;
        HoldReason = "Approved. The action engine still enforces consent, template, and the 24-hour window.";
        Touch();
    }

    public void MarkHeld(string detail)
    {
        Status = WhatsAppMessageStatus.Held;
        HoldReason = detail.Trim();
        Touch();
    }

    public void MarkFailed(string detail)
    {
        Status = WhatsAppMessageStatus.Failed;
        HoldReason = detail.Trim();
        Touch();
    }

    public void MarkQueued(string detail)
    {
        Status = WhatsAppMessageStatus.Queued;
        HoldReason = detail.Trim();
        Touch();
    }

    public void MarkDelivered(string providerMessageId)
    {
        Status = WhatsAppMessageStatus.Delivered;
        ProviderMessageId = providerMessageId;
        HoldReason = "Cloud API accepted the message.";
        Touch();
    }
}

public sealed class WhatsAppMessageAttempt : TenantOwnedEntity
{
    public Guid MessageId { get; private set; }
    public int Ordinal { get; private set; }
    public string Outcome { get; private set; } = string.Empty;
    public string Detail { get; private set; } = string.Empty;

    private WhatsAppMessageAttempt() { }

    public static WhatsAppMessageAttempt Record(Guid tenantId, Guid messageId, int ordinal, string outcome, string detail) =>
        new()
        {
            TenantId = tenantId,
            MessageId = messageId,
            Ordinal = ordinal,
            Outcome = outcome.Trim(),
            Detail = detail.Trim()
        };
}

public sealed class WhatsAppWebhookEvent : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string Detail { get; private set; } = string.Empty;
    public bool Trusted { get; private set; }

    private WhatsAppWebhookEvent() { }

    public static WhatsAppWebhookEvent Record(Guid tenantId, Guid businessId, string kind, string detail, bool trusted) =>
        new()
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Kind = kind.Trim(),
            Detail = detail.Trim(),
            Trusted = trusted
        };
}
