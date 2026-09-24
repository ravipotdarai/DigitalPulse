using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Projects;

public enum ProjectPermissionScope
{
    None = 0,
    Partial = 1,
    Full = 2
}

public enum ProjectConfidentiality
{
    Internal = 1,
    Restricted = 2,
    Public = 3
}

public enum ProjectPublicationStatus
{
    Draft = 1,
    Ready = 2,
    Archived = 3
}

public sealed class Project : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? ClientName { get; private set; }
    public string? Industry { get; private set; }
    public string? Location { get; private set; }
    public string? Description { get; private set; }
    public string? Outcomes { get; private set; }
    public DateOnly? StartedOn { get; private set; }
    public DateOnly? CompletedOn { get; private set; }
    public ProjectPermissionScope PermissionScope { get; private set; } = ProjectPermissionScope.None;
    public ProjectConfidentiality Confidentiality { get; private set; } = ProjectConfidentiality.Internal;
    public ProjectPublicationStatus PublicationStatus { get; private set; } = ProjectPublicationStatus.Draft;

    private Project() { }

    public static Project Create(
        Guid tenantId,
        Guid businessId,
        string name,
        string? clientName,
        string? industry,
        string? location,
        string? description,
        string? outcomes,
        DateOnly? startedOn,
        DateOnly? completedOn,
        ProjectPermissionScope permission,
        ProjectConfidentiality confidentiality)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Project
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Name = name.Trim(),
            ClientName = NullIfEmpty(clientName),
            Industry = NullIfEmpty(industry),
            Location = NullIfEmpty(location),
            Description = NullIfEmpty(description),
            Outcomes = NullIfEmpty(outcomes),
            StartedOn = startedOn,
            CompletedOn = completedOn,
            PermissionScope = permission,
            Confidentiality = confidentiality,
            PublicationStatus = ProjectPublicationStatus.Draft
        };
    }

    public void Update(
        string name,
        string? clientName,
        string? industry,
        string? location,
        string? description,
        string? outcomes,
        DateOnly? startedOn,
        DateOnly? completedOn,
        ProjectPermissionScope permission,
        ProjectConfidentiality confidentiality,
        ProjectPublicationStatus publication)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        ClientName = NullIfEmpty(clientName);
        Industry = NullIfEmpty(industry);
        Location = NullIfEmpty(location);
        Description = NullIfEmpty(description);
        Outcomes = NullIfEmpty(outcomes);
        StartedOn = startedOn;
        CompletedOn = completedOn;
        PermissionScope = permission;
        Confidentiality = confidentiality;
        PublicationStatus = publication;
        Touch();
    }

    public bool AllowsPublication(ContentVariantKind kind) =>
        PermissionScope switch
        {
            ProjectPermissionScope.None => false,
            ProjectPermissionScope.Partial => kind is ContentVariantKind.WebsiteCaseStudy
                or ContentVariantKind.WhatsAppTemplateDraft
                or ContentVariantKind.WhatsAppSessionMessage,
            ProjectPermissionScope.Full => Confidentiality != ProjectConfidentiality.Restricted
                || kind is ContentVariantKind.WebsiteCaseStudy
                    or ContentVariantKind.WhatsAppTemplateDraft
                    or ContentVariantKind.WhatsAppSessionMessage,
            _ => false
        };

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class ProjectServiceLink : TenantOwnedEntity
{
    public Guid ProjectId { get; private set; }
    public Guid ServiceId { get; private set; }

    private ProjectServiceLink() { }

    public static ProjectServiceLink Link(Guid tenantId, Guid projectId, Guid serviceId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (projectId == Guid.Empty) throw new ArgumentException("Project is required.", nameof(projectId));
        if (serviceId == Guid.Empty) throw new ArgumentException("Service is required.", nameof(serviceId));
        return new ProjectServiceLink { TenantId = tenantId, ProjectId = projectId, ServiceId = serviceId };
    }
}

public sealed class ProjectBrandLink : TenantOwnedEntity
{
    public Guid ProjectId { get; private set; }
    public Guid BrandId { get; private set; }

    private ProjectBrandLink() { }

    public static ProjectBrandLink Link(Guid tenantId, Guid projectId, Guid brandId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (projectId == Guid.Empty) throw new ArgumentException("Project is required.", nameof(projectId));
        if (brandId == Guid.Empty) throw new ArgumentException("Brand is required.", nameof(brandId));
        return new ProjectBrandLink { TenantId = tenantId, ProjectId = projectId, BrandId = brandId };
    }
}
