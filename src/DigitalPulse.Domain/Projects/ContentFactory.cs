using DigitalPulse.Domain.Common;

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
    WhatsAppSessionMessage = 12
}

public enum ContentItemStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Rejected = 4,
    Hold = 5
}

public enum ApprovalDecisionKind
{
    Approved = 1,
    Rejected = 2
}

public sealed class ContentItem : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public ContentItemStatus Status { get; private set; } = ContentItemStatus.Draft;
    public string SourceNote { get; private set; } = string.Empty;

    private ContentItem() { }

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
            Title = title.Trim(),
            Status = ContentItemStatus.Draft,
            SourceNote = "Assembled from the project record. This is not a live AI rewrite."
        };
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
