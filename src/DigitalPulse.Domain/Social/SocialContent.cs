using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Social;

public enum SocialContentKind
{
    GooglePost = 1,
    FacebookPost = 2,
    InstagramCaption = 3,
    LinkedInPost = 4,
    YouTubeMetadata = 5
}

public enum SocialContentStatus
{
    Draft = 1,
    Approved = 2,
    Assisted = 3,
    Blocked = 4,
    Failed = 5
}

public enum SocialVerificationStatus
{
    None = 0,
    Hold = 1,
    Failed = 2
}

public enum SocialMetricStatus
{
    Unavailable = 1,
    Hold = 2
}

public sealed class SocialContentItem : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public string PlatformCode { get; private set; } = string.Empty;
    public SocialContentKind Kind { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public SocialContentStatus Status { get; private set; } = SocialContentStatus.Draft;
    public SocialVerificationStatus VerificationStatus { get; private set; } = SocialVerificationStatus.None;
    public string? VerificationDetail { get; private set; }
    public string? LastPublishError { get; private set; }

    private SocialContentItem() { }

    public static SocialContentItem Draft(
        Guid tenantId,
        Guid businessId,
        string platformCode,
        SocialContentKind kind,
        string title,
        string body)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(platformCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        return new SocialContentItem
        {
            TenantId = tenantId,
            BusinessId = businessId,
            PlatformCode = platformCode.Trim().ToUpperInvariant(),
            Kind = kind,
            Title = title.Trim(),
            Body = body.Trim(),
            Status = SocialContentStatus.Draft
        };
    }

    public void UpdateDraft(string title, string body)
    {
        if (Status is not (SocialContentStatus.Draft or SocialContentStatus.Failed or SocialContentStatus.Blocked or SocialContentStatus.Assisted))
        {
            throw new InvalidOperationException("Only unpublished drafts can be edited.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        Title = title.Trim();
        Body = body.Trim();
        Status = SocialContentStatus.Draft;
        LastPublishError = null;
        Touch();
    }

    public void Approve()
    {
        Status = SocialContentStatus.Approved;
        Touch();
    }

    public void MarkAssisted(string detail)
    {
        Status = SocialContentStatus.Assisted;
        VerificationStatus = SocialVerificationStatus.Hold;
        VerificationDetail = detail.Trim();
        LastPublishError = null;
        Touch();
    }

    public void MarkBlocked(string reason)
    {
        Status = SocialContentStatus.Blocked;
        VerificationStatus = SocialVerificationStatus.Hold;
        VerificationDetail = reason.Trim();
        LastPublishError = reason.Trim();
        Touch();
    }

    public void MarkFailed(string reason)
    {
        Status = SocialContentStatus.Failed;
        VerificationStatus = SocialVerificationStatus.Failed;
        VerificationDetail = reason.Trim();
        LastPublishError = reason.Trim();
        Touch();
    }
}

public sealed class SocialMetricSnapshot : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public Guid? ConnectionId { get; private set; }
    public string PlatformCode { get; private set; } = string.Empty;
    public SocialMetricStatus Status { get; private set; }
    public string Detail { get; private set; } = string.Empty;
    public DateTimeOffset CapturedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private SocialMetricSnapshot() { }

    public static SocialMetricSnapshot Hold(
        Guid tenantId,
        Guid businessId,
        Guid? connectionId,
        string platformCode,
        SocialMetricStatus status,
        string detail)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(platformCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);

        return new SocialMetricSnapshot
        {
            TenantId = tenantId,
            BusinessId = businessId,
            ConnectionId = connectionId,
            PlatformCode = platformCode.Trim().ToUpperInvariant(),
            Status = status,
            Detail = detail.Trim(),
            CapturedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
