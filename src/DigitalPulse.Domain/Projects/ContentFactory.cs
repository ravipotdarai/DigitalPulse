using DigitalPulse.Domain.Common;
using DigitalPulse.Domain.Content;
using DigitalPulse.Domain.Safety;

namespace DigitalPulse.Domain.Projects;

public enum ContentVariantKind
{
    WebsiteCaseStudy = 1,
    GooglePost = 2,
    LinkedInPost = 3,
    InstagramCaption = 4,
    InstagramCarousel = 5,
    InstagramReelScript = 6,
    FacebookPost = 7,
    YouTubeMetadata = 8,
    IndiaMartContent = 9,
    JustdialContent = 10,
    WhatsAppTemplateDraft = 11,
    WhatsAppSessionMessage = 12,
    WebsiteArticle = 13,
    Newsletter = 14
}

public enum ContentItemStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Rejected = 4,
    Hold = 5,
    Scheduled = 6,
    Published = 7,
    Archived = 8
}

public enum ContentVisibility
{
    Private = 1,
    Public = 2
}

public enum ApprovalDecisionKind
{
    Approved = 1,
    Rejected = 2
}

public sealed class ContentItem : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid? ContentTypeId { get; private set; }
    public string ContentTypeCode { get; private set; } = "ARTICLE";
    public byte[] RowVersion { get; private set; } = [];
    public Guid? AuthorUserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Excerpt { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public ContentItemStatus Status { get; private set; } = ContentItemStatus.Draft;
    public ContentVisibility Visibility { get; private set; } = ContentVisibility.Private;
    public Guid? FeaturedMediaAssetId { get; private set; }
    public string? CanonicalUrl { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }
    public DateTimeOffset? ScheduledAtUtc { get; private set; }
    public string SourceNote { get; private set; } = string.Empty;

    private ContentItem() { }

    public static ContentItem Draft(
        Guid tenantId,
        Guid businessId,
        string contentTypeCode,
        string title,
        string? slug,
        string excerpt,
        string body,
        ContentVisibility visibility,
        Guid? projectId,
        Guid? authorUserId,
        Guid? featuredMediaAssetId,
        string? canonicalUrl)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(contentTypeCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        ContentSafety.EnsureAllowed(title, excerpt, body);

        return new ContentItem
        {
            TenantId = tenantId,
            BusinessId = businessId,
            ProjectId = projectId,
            ContentTypeCode = contentTypeCode.Trim().ToUpperInvariant(),
            AuthorUserId = authorUserId,
            Title = title.Trim(),
            Slug = ContentSlug.From(slug, title),
            Excerpt = excerpt.Trim(),
            Body = body.Trim(),
            Status = ContentItemStatus.Draft,
            Visibility = visibility,
            FeaturedMediaAssetId = featuredMediaAssetId,
            CanonicalUrl = string.IsNullOrWhiteSpace(canonicalUrl) ? null : canonicalUrl.Trim(),
            SourceNote = "Draft stored in the Content Hub. Nothing has been published."
        };
    }

    public static ContentItem FromProject(Guid tenantId, Guid businessId, Guid projectId, string title)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        if (projectId == Guid.Empty) throw new ArgumentException("Project is required.", nameof(projectId));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new ContentItem
        {
            TenantId = tenantId,
            BusinessId = businessId,
            ProjectId = projectId,
            ContentTypeCode = "PROJECT_STORY",
            Title = title.Trim(),
            Slug = ContentSlug.From(null, title),
            Excerpt = string.Empty,
            Body = string.Empty,
            Status = ContentItemStatus.Draft,
            Visibility = ContentVisibility.Private,
            SourceNote = "Assembled from the project record. This is not a live AI rewrite."
        };
    }

    public void UpdateDraft(
        string contentTypeCode,
        string title,
        string? slug,
        string excerpt,
        string body,
        ContentVisibility visibility,
        Guid? projectId,
        Guid? featuredMediaAssetId,
        string? canonicalUrl)
    {
        if (Status is ContentItemStatus.Published or ContentItemStatus.Archived)
        {
            throw new InvalidOperationException("Published or archived content cannot be overwritten. Create a revision first.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(contentTypeCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        ContentSafety.EnsureAllowed(title, excerpt, body);
        ContentTypeCode = contentTypeCode.Trim().ToUpperInvariant();
        Title = title.Trim();
        Slug = ContentSlug.From(slug, title);
        Excerpt = excerpt.Trim();
        Body = body.Trim();
        Visibility = visibility;
        ProjectId = projectId;
        FeaturedMediaAssetId = featuredMediaAssetId;
        CanonicalUrl = string.IsNullOrWhiteSpace(canonicalUrl) ? null : canonicalUrl.Trim();
        Status = ContentItemStatus.Draft;
        PublishedAtUtc = null;
        Touch();
    }

    public void AssignType(Guid? contentTypeId, string contentTypeCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentTypeCode);
        ContentTypeId = contentTypeId;
        ContentTypeCode = contentTypeCode.Trim().ToUpperInvariant();
        Touch();
    }

    public void RequestApproval()
    {
        if (Status is ContentItemStatus.Approved)
        {
            throw new InvalidOperationException("This pack is already approved.");
        }

        Status = ContentItemStatus.PendingApproval;
        Touch();
    }

    public void MarkApproved()
    {
        Status = ContentItemStatus.Approved;
        Touch();
    }

    public void MarkRejected(string note)
    {
        Status = ContentItemStatus.Rejected;
        SourceNote = string.IsNullOrWhiteSpace(note) ? SourceNote : note.Trim();
        Touch();
    }

    public void MarkHold(string note)
    {
        Status = ContentItemStatus.Hold;
        SourceNote = note.Trim();
        Touch();
    }

    public void SetFeaturedMedia(Guid? mediaAssetId)
    {
        FeaturedMediaAssetId = mediaAssetId;
        Touch();
    }

    public void ClearSchedule()
    {
        if (Status is not ContentItemStatus.Scheduled)
        {
            throw new InvalidOperationException("Only a scheduled article can cancel its schedule.");
        }

        ScheduledAtUtc = null;
        Status = ContentItemStatus.Approved;
        SourceNote = "Schedule cancelled. The article is still approved and unpublished.";
        Touch();
    }

    public void Schedule(DateTimeOffset atUtc)
    {
        if (Status is not (ContentItemStatus.Approved or ContentItemStatus.Scheduled))
        {
            throw new InvalidOperationException("Approve the draft before scheduling.");
        }

        if (atUtc <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("Schedule a time in the future.");
        }

        ScheduledAtUtc = atUtc;
        Status = ContentItemStatus.Scheduled;
        SourceNote = $"Scheduled for {atUtc:u}. Nothing has been published yet.";
        Touch();
    }

    public void Publish()
    {
        if (Status is not (ContentItemStatus.Approved or ContentItemStatus.Scheduled))
        {
            throw new InvalidOperationException("Approve the draft before publishing.");
        }

        Status = ContentItemStatus.Published;
        PublishedAtUtc = DateTimeOffset.UtcNow;
        SourceNote = Visibility == ContentVisibility.Public
            ? "Published on the DigitalPulse Content Hub."
            : "Marked published internally. Visibility is private, so it is not on the public hub.";
        Touch();
    }

    public void Archive()
    {
        Status = ContentItemStatus.Archived;
        Touch();
    }
}

public sealed class ContentVariant : TenantOwnedEntity
{
    public Guid ContentItemId { get; private set; }
    public ContentVariantKind Kind { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public ContentItemStatus Status { get; private set; } = ContentItemStatus.Draft;
    public string PublicationHold { get; private set; } = "Live platform write is not wired for the content factory.";

    private ContentVariant() { }

    public static ContentVariant Draft(Guid tenantId, Guid contentItemId, ContentVariantKind kind, string title, string body)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (contentItemId == Guid.Empty) throw new ArgumentException("Content is required.", nameof(contentItemId));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        return new ContentVariant
        {
            TenantId = tenantId,
            ContentItemId = contentItemId,
            Kind = kind,
            Title = title.Trim(),
            Body = body.Trim(),
            Status = ContentItemStatus.Draft
        };
    }

    public void ApproveForScope(bool allowed)
    {
        Status = allowed ? ContentItemStatus.Approved : ContentItemStatus.Hold;
        PublicationHold = allowed
            ? "Approved internally. Live publish stays on hold until a provider write exists."
            : "Outside this project's permission scope. The variant stays unpublished.";
        Touch();
    }

    public void Reject()
    {
        Status = ContentItemStatus.Rejected;
        Touch();
    }
}

public sealed class ApprovalRequest : TenantOwnedEntity
{
    public Guid ContentItemId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public bool Open { get; private set; } = true;

    private ApprovalRequest() { }

    public static ApprovalRequest OpenFor(Guid tenantId, Guid contentItemId, string reason)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (contentItemId == Guid.Empty) throw new ArgumentException("Content is required.", nameof(contentItemId));
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new ApprovalRequest
        {
            TenantId = tenantId,
            ContentItemId = contentItemId,
            Reason = reason.Trim(),
            Open = true
        };
    }

    public void Close()
    {
        Open = false;
        Touch();
    }
}

public sealed class ApprovalDecision : TenantOwnedEntity
{
    public Guid ApprovalRequestId { get; private set; }
    public ApprovalDecisionKind Kind { get; private set; }
    public string Note { get; private set; } = string.Empty;

    private ApprovalDecision() { }

    public static ApprovalDecision Record(Guid tenantId, Guid approvalRequestId, ApprovalDecisionKind kind, string note)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (approvalRequestId == Guid.Empty) throw new ArgumentException("Approval is required.", nameof(approvalRequestId));
        ArgumentException.ThrowIfNullOrWhiteSpace(note);

        return new ApprovalDecision
        {
            TenantId = tenantId,
            ApprovalRequestId = approvalRequestId,
            Kind = kind,
            Note = note.Trim()
        };
    }
}

public static class ProjectContentFactory
{
    public static IReadOnlyList<(ContentVariantKind Kind, string Title, string Body)> Build(Project project, IReadOnlyList<string> services)
    {
        var who = project.ClientName ?? "the client";
        var where = project.Location ?? "the recorded location";
        var what = project.Description ?? "No description is stored yet.";
        var outcome = project.Outcomes ?? "Outcomes are not recorded yet.";
        var serviceLine = services.Count == 0 ? "No linked services." : string.Join(", ", services);
        var headline = project.Name;

        return
        [
            (ContentVariantKind.WebsiteCaseStudy, $"{headline} case study", $"Case study for {who} in {where}. {what} Outcome: {outcome} Services: {serviceLine}"),
            (ContentVariantKind.GooglePost, $"{headline} update", $"{headline} for {who}. {outcome}"),
            (ContentVariantKind.LinkedInPost, $"{headline}", $"{headline} with {who} in {where}. {what}"),
            (ContentVariantKind.InstagramCaption, headline, $"{headline} — {outcome}"),
            (ContentVariantKind.InstagramCarousel, $"{headline} carousel", $"Slide 1: {headline}. Slide 2: {what}. Slide 3: {outcome}"),
            (ContentVariantKind.InstagramReelScript, $"{headline} reel", $"Open on the work. Voiceover: {what} Close on: {outcome}"),
            (ContentVariantKind.FacebookPost, headline, $"{headline} for {who}. {outcome}"),
            (ContentVariantKind.YouTubeMetadata, headline, $"Title: {headline}. Description: {what} Tags: {serviceLine}"),
            (ContentVariantKind.IndiaMartContent, headline, $"{headline} | {serviceLine} | {where}. {what}"),
            (ContentVariantKind.JustdialContent, headline, $"{headline} in {where}. {outcome}"),
            (ContentVariantKind.WhatsAppTemplateDraft, $"{headline} template", $"Hello {who}, this is a template draft about {headline}. It has not been submitted to WhatsApp."),
            (ContentVariantKind.WhatsAppSessionMessage, $"{headline} session", $"Following up on {headline}. This is a session-message draft only.")
        ];
    }
}
