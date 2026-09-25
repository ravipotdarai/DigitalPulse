using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Projects;

public enum MediaKind
{
    Image = 1,
    Video = 2,
    Document = 3
}

public sealed class MediaAsset : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public MediaKind Kind { get; private set; }
    public string? SourceUrl { get; private set; }
    public string Note { get; private set; } = string.Empty;

    private MediaAsset() { }

    public static MediaAsset Register(Guid tenantId, Guid businessId, string label, MediaKind kind, string? sourceUrl, Guid? id = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        return new MediaAsset
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = tenantId,
            BusinessId = businessId,
            Label = label.Trim(),
            Kind = kind,
            SourceUrl = string.IsNullOrWhiteSpace(sourceUrl) ? null : sourceUrl.Trim(),
            Note = "Stored on this host so DigitalPulse can send it through the official publish API after the user approves."
        };
    }
}

public sealed class ProjectMedia : TenantOwnedEntity
{
    public Guid ProjectId { get; private set; }
    public Guid MediaAssetId { get; private set; }

    private ProjectMedia() { }

    public static ProjectMedia Attach(Guid tenantId, Guid projectId, Guid mediaAssetId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (projectId == Guid.Empty) throw new ArgumentException("Project is required.", nameof(projectId));
        if (mediaAssetId == Guid.Empty) throw new ArgumentException("Media is required.", nameof(mediaAssetId));
        return new ProjectMedia { TenantId = tenantId, ProjectId = projectId, MediaAssetId = mediaAssetId };
    }
}
