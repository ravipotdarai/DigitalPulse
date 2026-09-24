using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Agency;

public enum AgencyClientStatus
{
    Prospect = 0,
    Active = 1,
    Paused = 2,
    Archived = 3
}

public enum AgencyWorkflowKind
{
    ClientOnboarding = 0,
    MonthlyReview = 1,
    PresenceAudit = 2,
    WhiteLabelReview = 3
}

public enum AgencyWorkflowStatus
{
    Draft = 0,
    InProgress = 1,
    Waiting = 2,
    Held = 3,
    Completed = 4
}

public enum AgencyReportScope
{
    Client = 0,
    Portfolio = 1
}

public enum AgencyReportLineKind
{
    ObservedFact = 0,
    Recommendation = 1,
    AiInterpretation = 2,
    CustomerDecision = 3
}

public static class AgencyPolicy
{
    public const string AgencyOnly = "Agency workspace is available on Agency tenants.";
    public const string WhiteLabelDenied = "White-label configuration is an Agency plan entitlement.";
    public const string CustomDomainHold =
        "Custom-domain hosting is not configured. The stored hostname is not live and DNS is not invented.";
    public const string BrandingStored =
        "White-label branding is stored on this tenant. Custom-domain hosting stays held until a live host is configured.";
    public const string AiInterpretationHold =
        "AI Interpretation stays held until an evidence-backed orchestrator run exists for this client.";
    public const string ClientIsolation =
        "Agency work is scoped to a client business on this tenant. Cross-tenant client ids are not visible.";

    public static void EnsureAgencyTenant(Tenancy.TenantType type)
    {
        if (type != Tenancy.TenantType.Agency)
        {
            throw new InvalidOperationException(AgencyOnly);
        }
    }

    public static void EnsureWhiteLabel(Billing.SubscriptionPlan plan)
    {
        if (!plan.WhiteLabel)
        {
            throw new InvalidOperationException(WhiteLabelDenied);
        }
    }

    public static bool CanTransition(AgencyClientStatus from, AgencyClientStatus to) =>
        from == to || (from, to) switch
        {
            (AgencyClientStatus.Prospect, AgencyClientStatus.Active) => true,
            (AgencyClientStatus.Prospect, AgencyClientStatus.Archived) => true,
            (AgencyClientStatus.Active, AgencyClientStatus.Paused) => true,
            (AgencyClientStatus.Active, AgencyClientStatus.Archived) => true,
            (AgencyClientStatus.Paused, AgencyClientStatus.Active) => true,
            (AgencyClientStatus.Paused, AgencyClientStatus.Archived) => true,
            (AgencyClientStatus.Archived, AgencyClientStatus.Active) => true,
            (AgencyClientStatus.Archived, AgencyClientStatus.Prospect) => true,
            _ => false
        };

    public static void EnsureTransition(AgencyClientStatus from, AgencyClientStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidOperationException($"Cannot move a client from {from} to {to}.");
        }
    }

    public static string[] StepsFor(AgencyWorkflowKind kind) => kind switch
    {
        AgencyWorkflowKind.ClientOnboarding =>
            ["Capture client identity", "Confirm first location", "Review canonical facts", "Mark client active"],
        AgencyWorkflowKind.MonthlyReview =>
            ["Collect stored metrics", "Assemble client report", "Record customer decision", "Close the review"],
        AgencyWorkflowKind.PresenceAudit =>
            ["Review stored scans", "Review monitoring alerts", "Recommend next actions", "Close the audit"],
        AgencyWorkflowKind.WhiteLabelReview =>
            ["Confirm display name", "Confirm support contact", "Review custom domain", "Store branding"],
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static string NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return "#111111";
        }

        var value = color.Trim();
        if (value.Length != 7 || value[0] != '#')
        {
            throw new ArgumentException("Primary color must be a hex value like #1A2B3C.");
        }

        foreach (var ch in value.AsSpan(1))
        {
            var hex = (ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'F') || (ch >= 'a' && ch <= 'f');
            if (!hex)
            {
                throw new ArgumentException("Primary color must be a hex value like #1A2B3C.");
            }
        }

        return value.ToUpperInvariant();
    }

    public static string? NormalizeDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return null;
        }

        var value = domain.Trim().ToLowerInvariant();
        if (value.Contains("://", StringComparison.Ordinal) || value.Contains('/') || value.Contains(' '))
        {
            throw new ArgumentException("Custom domain must be a hostname such as reports.agency.example.");
        }

        return value;
    }

    public static bool LooksLikeEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains('@') && email.Contains('.');

    public static AgencyClientStatus ParseStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return AgencyClientStatus.Prospect;
        }

        if (Enum.TryParse<AgencyClientStatus>(value, true, out var status))
        {
            return status;
        }

        throw new ArgumentException("Client status must be Prospect, Active, Paused, or Archived.");
    }

    public static AgencyWorkflowKind ParseWorkflowKind(string? value)
    {
        if (Enum.TryParse<AgencyWorkflowKind>(value, true, out var kind))
        {
            return kind;
        }

        throw new ArgumentException("Workflow kind must be ClientOnboarding, MonthlyReview, PresenceAudit, or WhiteLabelReview.");
    }

    public static AgencyReportScope ParseScope(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return AgencyReportScope.Portfolio;
        }

        if (Enum.TryParse<AgencyReportScope>(value, true, out var scope))
        {
            return scope;
        }

        throw new ArgumentException("Report scope must be Client or Portfolio.");
    }
}

public sealed class AgencyClient : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public AgencyClientStatus Status { get; private set; } = AgencyClientStatus.Active;
    public string? ContactName { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? Notes { get; private set; }
    public string? ExternalRef { get; private set; }

    private AgencyClient() { }

    public static AgencyClient Enroll(
        Guid tenantId,
        Guid businessId,
        AgencyClientStatus status = AgencyClientStatus.Active,
        string? contactName = null,
        string? contactEmail = null,
        string? notes = null,
        string? externalRef = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        if (!string.IsNullOrWhiteSpace(contactEmail) && !AgencyPolicy.LooksLikeEmail(contactEmail))
        {
            throw new ArgumentException("Contact email is invalid.", nameof(contactEmail));
        }

        return new AgencyClient
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Status = status,
            ContactName = BlankToNull(contactName),
            ContactEmail = BlankToNull(contactEmail),
            Notes = BlankToNull(notes),
            ExternalRef = BlankToNull(externalRef)
        };
    }

    public void Update(
        AgencyClientStatus status,
        string? contactName,
        string? contactEmail,
        string? notes,
        string? externalRef)
    {
        AgencyPolicy.EnsureTransition(Status, status);
        if (!string.IsNullOrWhiteSpace(contactEmail) && !AgencyPolicy.LooksLikeEmail(contactEmail))
        {
            throw new ArgumentException("Contact email is invalid.", nameof(contactEmail));
        }

        Status = status;
        ContactName = BlankToNull(contactName);
        ContactEmail = BlankToNull(contactEmail);
        Notes = BlankToNull(notes);
        ExternalRef = BlankToNull(externalRef);
        Touch();
    }

    private static string? BlankToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class WhiteLabelProfile : TenantOwnedEntity
{
    public string DisplayName { get; private set; } = "Agency";
    public string? SupportEmail { get; private set; }
    public string? SupportPhone { get; private set; }
    public string PrimaryColor { get; private set; } = "#111111";
    public string? LogoUrl { get; private set; }
    public string? CustomDomain { get; private set; }
    public bool Enabled { get; private set; }
    public string HoldReason { get; private set; } = AgencyPolicy.BrandingStored;

    private WhiteLabelProfile() { }

    public static WhiteLabelProfile Create(Guid tenantId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        return new WhiteLabelProfile { TenantId = tenantId };
    }

    public void Apply(
        string displayName,
        string? supportEmail,
        string? supportPhone,
        string? primaryColor,
        string? logoUrl,
        string? customDomain,
        bool enabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (!string.IsNullOrWhiteSpace(supportEmail) && !AgencyPolicy.LooksLikeEmail(supportEmail))
        {
            throw new ArgumentException("Support email is invalid.", nameof(supportEmail));
        }

        DisplayName = displayName.Trim();
        SupportEmail = string.IsNullOrWhiteSpace(supportEmail) ? null : supportEmail.Trim();
        SupportPhone = string.IsNullOrWhiteSpace(supportPhone) ? null : supportPhone.Trim();
        PrimaryColor = AgencyPolicy.NormalizeColor(primaryColor);
        LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim();
        CustomDomain = AgencyPolicy.NormalizeDomain(customDomain);
        Enabled = enabled;
        HoldReason = CustomDomain is null ? AgencyPolicy.BrandingStored : AgencyPolicy.CustomDomainHold;
        Touch();
    }
}

public sealed class AgencyWorkflow : TenantOwnedEntity
{
    public Guid? ClientId { get; private set; }
    public Guid? BusinessId { get; private set; }
    public AgencyWorkflowKind Kind { get; private set; }
    public AgencyWorkflowStatus Status { get; private set; } = AgencyWorkflowStatus.Draft;
    public int CurrentStep { get; private set; }
    public string CurrentStepName { get; private set; } = string.Empty;
    public string HoldReason { get; private set; } = string.Empty;

    private AgencyWorkflow() { }

    public static AgencyWorkflow Start(Guid tenantId, AgencyWorkflowKind kind, Guid? clientId, Guid? businessId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        var steps = AgencyPolicy.StepsFor(kind);
        return new AgencyWorkflow
        {
            TenantId = tenantId,
            ClientId = clientId,
            BusinessId = businessId,
            Kind = kind,
            Status = AgencyWorkflowStatus.InProgress,
            CurrentStep = 0,
            CurrentStepName = steps[0],
            HoldReason = string.Empty
        };
    }

    public IReadOnlyList<AgencyWorkflowStep> CreateSteps()
    {
        var names = AgencyPolicy.StepsFor(Kind);
        return names.Select((name, index) => AgencyWorkflowStep.Create(TenantId, Id, index, name)).ToList();
    }

    public void Advance(IReadOnlyList<AgencyWorkflowStep> steps, string? note, bool hold, string? holdReason)
    {
        if (Status == AgencyWorkflowStatus.Completed)
        {
            throw new InvalidOperationException("This workflow is already completed.");
        }

        if (hold)
        {
            Status = AgencyWorkflowStatus.Held;
            HoldReason = string.IsNullOrWhiteSpace(holdReason)
                ? "Held until stored evidence or live hosting exists."
                : holdReason.Trim();
            Touch();
            return;
        }

        var ordered = steps.OrderBy(s => s.Ordinal).ToList();
        var current = ordered.FirstOrDefault(s => s.Ordinal == CurrentStep)
            ?? throw new InvalidOperationException("Workflow step was not found.");
        current.Complete(note);
        if (CurrentStep >= ordered.Count - 1)
        {
            Status = AgencyWorkflowStatus.Completed;
            HoldReason = string.Empty;
            Touch();
            return;
        }

        CurrentStep++;
        CurrentStepName = ordered.First(s => s.Ordinal == CurrentStep).Name;
        Status = AgencyWorkflowStatus.InProgress;
        HoldReason = string.Empty;
        Touch();
    }
}

public sealed class AgencyWorkflowStep : TenantOwnedEntity
{
    public Guid WorkflowId { get; private set; }
    public int Ordinal { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool Completed { get; private set; }
    public string? Note { get; private set; }

    private AgencyWorkflowStep() { }

    public static AgencyWorkflowStep Create(Guid tenantId, Guid workflowId, int ordinal, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new AgencyWorkflowStep
        {
            TenantId = tenantId,
            WorkflowId = workflowId,
            Ordinal = ordinal,
            Name = name.Trim()
        };
    }

    public void Complete(string? note)
    {
        Completed = true;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        Touch();
    }
}

public sealed class AgencyReport : TenantOwnedEntity
{
    public AgencyReportScope Scope { get; private set; }
    public Guid? ClientId { get; private set; }
    public Guid? BusinessId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string ObservedFact { get; private set; } = string.Empty;
    public string Recommendation { get; private set; } = string.Empty;
    public string AiInterpretation { get; private set; } = string.Empty;
    public string? CustomerDecision { get; private set; }
    public string HoldReason { get; private set; } = string.Empty;

    private AgencyReport() { }

    public static AgencyReport Assemble(
        Guid tenantId,
        AgencyReportScope scope,
        Guid? clientId,
        Guid? businessId,
        string title,
        string observedFact,
        string recommendation,
        string aiInterpretation,
        string holdReason)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (scope == AgencyReportScope.Client && (clientId is null || businessId is null))
        {
            throw new InvalidOperationException("A client report must name one client business on this tenant.");
        }

        return new AgencyReport
        {
            TenantId = tenantId,
            Scope = scope,
            ClientId = clientId,
            BusinessId = businessId,
            Title = title.Trim(),
            ObservedFact = observedFact.Trim(),
            Recommendation = recommendation.Trim(),
            AiInterpretation = aiInterpretation.Trim(),
            HoldReason = holdReason.Trim()
        };
    }

    public void RecordDecision(string decision)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decision);
        CustomerDecision = decision.Trim();
        Touch();
    }
}

public sealed class AgencyReportLine : TenantOwnedEntity
{
    public Guid ReportId { get; private set; }
    public Guid? BusinessId { get; private set; }
    public AgencyReportLineKind Kind { get; private set; }
    public string Body { get; private set; } = string.Empty;

    private AgencyReportLine() { }

    public static AgencyReportLine Create(
        Guid tenantId,
        Guid reportId,
        AgencyReportLineKind kind,
        string body,
        Guid? businessId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        return new AgencyReportLine
        {
            TenantId = tenantId,
            ReportId = reportId,
            BusinessId = businessId,
            Kind = kind,
            Body = body.Trim()
        };
    }
}
