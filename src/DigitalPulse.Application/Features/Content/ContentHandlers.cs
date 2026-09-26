using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Ai;
using DigitalPulse.Application.Features.Identity;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Domain.Ai;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Content;
using DigitalPulse.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Content;

internal static class ContentHubMaps
{
    public static ContentVisibility ParseVisibility(string value) =>
        Enum.TryParse<ContentVisibility>(value, true, out var parsed)
            ? parsed
            : throw AppException.Validation("Visibility must be Public or Private.");

    public static HubContentSummary ToSummary(this ContentItem item, ContentSeoAnalysis? seo) =>
        new(item.Id, item.ContentTypeCode, item.Title, item.Slug, item.Status.ToString(), item.Visibility.ToString(), item.UpdatedAtUtc, seo?.SeoScore ?? 0, item.Excerpt, item.PublishedAtUtc);
}

public sealed class GetContentHubHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetContentHubHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ContentHubWorkspace> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        return await ContentComposer.WorkspaceAsync(_db, businessId, cancellationToken);
    }
}

public sealed class GetHubContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetHubContentHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<HubContentResponse> Handle(Guid businessId, Guid contentId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        return await ContentComposer.LoadAsync(_db, businessId, contentId, cancellationToken)
            ?? throw AppException.NotFound("Content was not found.");
    }
}

public sealed class CreateHubContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;
    private readonly ISearchProvider _search;

    public CreateHubContentHandler(IAppDbContext db, ITenantContext tenant, ICurrentUser user, ISearchProvider search)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
        _search = search;
    }

    public async Task<HubContentResponse> Handle(Guid businessId, CreateHubContentRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        if (!ContentTypeCatalog.Exists(request.ContentTypeCode))
        {
            throw AppException.Validation("Choose a catalog content type.");
        }

        ContentItem item;
        try
        {
            item = ContentItem.Draft(
                tenantId,
                businessId,
                request.ContentTypeCode,
                request.Title,
                request.Slug,
                ContentComposer.ExcerptOf(request.Excerpt, request.Body),
                request.Body,
                ContentHubMaps.ParseVisibility(request.Visibility),
                request.ProjectId,
                _user.IsAuthenticated ? _user.UserId : null,
                request.FeaturedMediaAssetId,
                request.CanonicalUrl);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            throw AppException.Validation(ex.Message);
        }

        await ContentComposer.EnsureUniqueSlugAsync(_db, businessId, item.Slug, null, cancellationToken);
        await ContentComposer.AssignTypeAsync(_db, item, request.ContentTypeCode, cancellationToken);
        _db.ContentItems.Add(item);
        await ContentComposer.EnsureFeaturedAsync(_db, tenantId, businessId, item, request.FeaturedMediaAssetId, cancellationToken);
        await ContentComposer.SyncTaxonomyAsync(_db, tenantId, businessId, item.Id, request.Categories, request.Tags, cancellationToken);
        await ContentComposer.AnalyzeAndStoreAsync(_db, tenantId, item, request.FocusKeyword, cancellationToken);
        ContentComposer.Revise(_db, tenantId, item, 1, "Initial draft", _user.IsAuthenticated ? _user.UserId : null);
        await ContentComposer.IndexAsync(_db, _search, tenantId, item, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return (await ContentComposer.LoadAsync(_db, businessId, item.Id, cancellationToken))!;
    }
}

public sealed class UpdateHubContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;
    private readonly ISearchProvider _search;

    public UpdateHubContentHandler(IAppDbContext db, ITenantContext tenant, ICurrentUser user, ISearchProvider search)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
        _search = search;
    }

    public async Task<HubContentResponse> Handle(Guid businessId, Guid contentId, UpdateHubContentRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.ContentItems.FirstOrDefaultAsync(c => c.Id == contentId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Content was not found.");
        if (!ContentTypeCatalog.Exists(request.ContentTypeCode))
        {
            throw AppException.Validation("Choose a catalog content type.");
        }

        try
        {
            item.UpdateDraft(
                request.ContentTypeCode,
                request.Title,
                request.Slug,
                ContentComposer.ExcerptOf(request.Excerpt, request.Body),
                request.Body,
                ContentHubMaps.ParseVisibility(request.Visibility),
                request.ProjectId,
                request.FeaturedMediaAssetId ?? item.FeaturedMediaAssetId,
                request.CanonicalUrl);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            throw AppException.Validation(ex.Message);
        }

        await ContentComposer.EnsureUniqueSlugAsync(_db, businessId, item.Slug, item.Id, cancellationToken);
        await ContentComposer.AssignTypeAsync(_db, item, request.ContentTypeCode, cancellationToken);
        await ContentComposer.EnsureFeaturedAsync(_db, tenantId, businessId, item, request.FeaturedMediaAssetId ?? item.FeaturedMediaAssetId, cancellationToken);
        await ContentComposer.SyncTaxonomyAsync(_db, tenantId, businessId, item.Id, request.Categories, request.Tags, cancellationToken);
        await ContentComposer.AnalyzeAndStoreAsync(_db, tenantId, item, request.FocusKeyword, cancellationToken);
        var version = await _db.ContentRevisions.CountAsync(r => r.ContentItemId == item.Id, cancellationToken) + 1;
        ContentComposer.Revise(_db, tenantId, item, version, request.ChangeSummary ?? "Edited", _user.IsAuthenticated ? _user.UserId : null);
        await ContentComposer.IndexAsync(_db, _search, tenantId, item, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return (await ContentComposer.LoadAsync(_db, businessId, item.Id, cancellationToken))!;
    }
}

public sealed class DeleteHubContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public DeleteHubContentHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task Handle(Guid businessId, Guid contentId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.ContentItems.FirstOrDefaultAsync(c => c.Id == contentId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Content was not found.");
        if (item.Status == ContentItemStatus.Published)
        {
            throw AppException.Validation("Published hub articles are archived, not deleted.");
        }

        _db.ContentItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ApproveHubContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public ApproveHubContentHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<HubContentResponse> Handle(Guid businessId, Guid contentId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.ContentItems.FirstOrDefaultAsync(c => c.Id == contentId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Content was not found.");
        ContentGuard.Require(item.Title, item.Excerpt, item.Body);
        item.RequestApproval();
        var request = ApprovalRequest.OpenFor(tenantId, item.Id, "Content Hub approval.");
        _db.ApprovalRequests.Add(request);
        item.MarkApproved();
        _db.ApprovalDecisions.Add(ApprovalDecision.Record(tenantId, request.Id, ApprovalDecisionKind.Approved, "Approved in the Content Hub. Live distribution still needs an official connection."));
        request.Close();
        await _db.SaveChangesAsync(cancellationToken);
        return (await ContentComposer.LoadAsync(_db, businessId, item.Id, cancellationToken))!;
    }
}

public sealed class ScheduleHubContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;

    public ScheduleHubContentHandler(IAppDbContext db, ITenantContext tenant, ICurrentUser user)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
    }

    public async Task<HubContentResponse> Handle(Guid businessId, Guid contentId, ScheduleHubContentRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.ContentItems.FirstOrDefaultAsync(c => c.Id == contentId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Content was not found.");
        try
        {
            item.Schedule(request.ScheduledAtUtc);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        _db.ContentCalendar.Add(ContentCalendarEntry.Schedule(
            tenantId,
            businessId,
            item.Id,
            request.ScheduledAtUtc,
            request.Channel ?? "HUB",
            null,
            _user.IsAuthenticated ? _user.UserId : null));
        await _db.SaveChangesAsync(cancellationToken);
        return (await ContentComposer.LoadAsync(_db, businessId, item.Id, cancellationToken))!;
    }
}

public sealed class PublishHubContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ISearchProvider _search;

    public PublishHubContentHandler(IAppDbContext db, ITenantContext tenant, ISearchProvider search)
    {
        _db = db;
        _tenant = tenant;
        _search = search;
    }

    public async Task<HubContentResponse> Handle(Guid businessId, Guid contentId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.ContentItems.FirstOrDefaultAsync(c => c.Id == contentId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Content was not found.");
        ContentGuard.Require(item.Title, item.Excerpt, item.Body);
        try
        {
            item.Publish();
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        var calendar = await _db.ContentCalendar.Where(c => c.ContentItemId == item.Id && c.Status == "Scheduled").ToListAsync(cancellationToken);
        foreach (var entry in calendar) entry.MarkPublished();
        _db.ContentMetrics.Add(ContentMetric.Hold(tenantId, businessId, item.Id, "Views are not invented. Metrics appear only after an official provider returns them."));
        await ContentComposer.IndexAsync(_db, _search, tenantId, item, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return (await ContentComposer.LoadAsync(_db, businessId, item.Id, cancellationToken))!;
    }
}

public sealed class ArchiveHubContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public ArchiveHubContentHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<HubContentResponse> Handle(Guid businessId, Guid contentId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.ContentItems.FirstOrDefaultAsync(c => c.Id == contentId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Content was not found.");
        item.Archive();
        await _db.SaveChangesAsync(cancellationToken);
        return (await ContentComposer.LoadAsync(_db, businessId, item.Id, cancellationToken))!;
    }
}

public sealed class ReleaseScheduledHubContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public ReleaseScheduledHubContentHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ContentHubWorkspace> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var due = await _db.ContentItems
            .Where(c => c.BusinessId == businessId && c.Status == ContentItemStatus.Scheduled && c.ScheduledAtUtc != null && c.ScheduledAtUtc <= now)
            .ToListAsync(cancellationToken);
        foreach (var item in due)
        {
            ContentGuard.Require(item.Title, item.Excerpt, item.Body);
            item.Publish();
            foreach (var entry in await _db.ContentCalendar.Where(c => c.ContentItemId == item.Id && c.Status == "Scheduled").ToListAsync(cancellationToken))
            {
                entry.MarkPublished();
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await ContentComposer.WorkspaceAsync(_db, businessId, cancellationToken);
    }
}

public sealed class AnalyzeHubSeoHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public AnalyzeHubSeoHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<HubContentResponse> Handle(Guid businessId, Guid contentId, string? focusKeyword, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.ContentItems.FirstOrDefaultAsync(c => c.Id == contentId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Content was not found.");
        await ContentComposer.AnalyzeAndStoreAsync(_db, tenantId, item, focusKeyword, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return (await ContentComposer.LoadAsync(_db, businessId, item.Id, cancellationToken))!;
    }
}

public sealed class CreateHubVariantsHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public CreateHubVariantsHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<HubContentResponse> Handle(Guid businessId, Guid contentId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.ContentItems.FirstOrDefaultAsync(c => c.Id == contentId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Content was not found.");
        if (await _db.ContentVariants.AnyAsync(v => v.ContentItemId == item.Id, cancellationToken))
        {
            return (await ContentComposer.LoadAsync(_db, businessId, item.Id, cancellationToken))!;
        }

        var excerpt = string.IsNullOrWhiteSpace(item.Excerpt) ? item.Body : item.Excerpt;
        (ContentVariantKind Kind, string Title, string Body)[] packs =
        [
            (ContentVariantKind.WebsiteArticle, item.Title, item.Body),
            (ContentVariantKind.GooglePost, item.Title, excerpt),
            (ContentVariantKind.LinkedInPost, item.Title, excerpt),
            (ContentVariantKind.FacebookPost, item.Title, excerpt),
            (ContentVariantKind.InstagramCaption, item.Title, excerpt),
            (ContentVariantKind.Newsletter, item.Title, excerpt)
        ];
        foreach (var (kind, title, body) in packs)
        {
            _db.ContentVariants.Add(ContentVariant.Draft(tenantId, item.Id, kind, title, body));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (await ContentComposer.LoadAsync(_db, businessId, item.Id, cancellationToken))!;
    }
}

public sealed class DistributeHubContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public DistributeHubContentHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<HubContentResponse> Handle(Guid businessId, Guid contentId, string providerCode, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.ContentItems.FirstOrDefaultAsync(c => c.Id == contentId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Content was not found.");
        if (item.Status is not (ContentItemStatus.Approved or ContentItemStatus.Published or ContentItemStatus.Scheduled))
        {
            throw AppException.Validation("Approve the article before distribution.");
        }

        var code = string.IsNullOrWhiteSpace(providerCode) ? "HUB" : providerCode.Trim().ToUpperInvariant();
        var link = await _db.Connections.FirstOrDefaultAsync(c => c.BusinessId == businessId && c.PlatformCode == code, cancellationToken);
        var distribution = ContentDistribution.Start(tenantId, businessId, item.Id, code, null, link?.Id);
        if (code == "HUB")
        {
            if (item.Status != ContentItemStatus.Published) item.Publish();
            distribution.MarkPublished(item.Slug);
        }
        else if (link is null || !link.HasLiveCredential)
        {
            distribution.Hold("No live official credential for this provider. Distribution stays assisted — DigitalPulse will not invent a post.");
        }
        else
        {
            distribution.Hold("Official write for long-form hub articles is not wired on this provider. Use Social compose for short posts.");
        }

        _db.ContentDistributions.Add(distribution);
        await _db.SaveChangesAsync(cancellationToken);
        return (await ContentComposer.LoadAsync(_db, businessId, item.Id, cancellationToken))!;
    }
}

public sealed class DiscoverContentOpportunitiesHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public DiscoverContentOpportunitiesHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ContentHubWorkspace> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var titles = await _db.ContentItems.AsNoTracking().Where(c => c.BusinessId == businessId).Select(c => c.Title).ToListAsync(cancellationToken);
        var services = await _db.Services.AsNoTracking().Where(s => s.BusinessId == businessId).ToListAsync(cancellationToken);
        var projects = await _db.Projects.AsNoTracking().Where(p => p.BusinessId == businessId).ToListAsync(cancellationToken);
        var findings = await _db.Findings.AsNoTracking().Where(f => f.BusinessId == businessId).OrderByDescending(f => f.CreatedAtUtc).Take(8).ToListAsync(cancellationToken);

        foreach (var service in services)
        {
            await ContentComposer.OfferAsync(_db, tenantId, businessId, $"How to choose {service.Name}", service.Description ?? $"A guide drawn from the stored {service.Name} service.", ContentOpportunitySource.Service, titles, cancellationToken);
        }

        foreach (var project in projects)
        {
            await ContentComposer.OfferAsync(_db, tenantId, businessId, $"{project.Name} case study", project.Description ?? "A project story from the stored record.", ContentOpportunitySource.Project, titles, cancellationToken);
        }

        foreach (var finding in findings)
        {
            await ContentComposer.OfferAsync(_db, tenantId, businessId, finding.Title, finding.Description, ContentOpportunitySource.Finding, titles, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await ContentComposer.WorkspaceAsync(_db, businessId, cancellationToken);
    }
}

public sealed class GenerateHubContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;
    private readonly IAiProvider _ai;
    private readonly ISearchProvider _search;

    public GenerateHubContentHandler(IAppDbContext db, ITenantContext tenant, ICurrentUser user, IAiProvider ai, ISearchProvider search)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
        _ai = ai;
        _search = search;
    }

    public async Task<HubContentResponse> Handle(Guid businessId, GenerateHubContentRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var business = await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        ContentGuard.Require(request.Prompt);
        var facts = await _db.Facts.AsNoTracking().Where(f => f.BusinessId == businessId && f.Status == FactStatus.Approved).ToListAsync(cancellationToken);
        var services = await _db.Services.AsNoTracking().Where(s => s.BusinessId == businessId).Select(s => s.Name).ToListAsync(cancellationToken);
        var projects = await _db.Projects.AsNoTracking().Where(p => p.BusinessId == businessId).Select(p => p.Name).ToListAsync(cancellationToken);
        var evidence = facts.Select(f => new AiEvidence("fact", f.FactTypeCode, f.Value, false, true)).ToList();
        var prompt = request.Prompt.Trim();
        if (Guid.TryParse(request.OpportunityId, out var opportunityId))
        {
            var opportunity = await _db.ContentOpportunities.FirstOrDefaultAsync(o => o.Id == opportunityId && o.BusinessId == businessId, cancellationToken);
            var topic = opportunity is null ? null : await _db.ContentTopics.FirstOrDefaultAsync(t => t.Id == opportunity.ContentTopicId, cancellationToken);
            if (topic is not null)
            {
                prompt = topic.Topic;
                opportunity?.Accept();
            }
        }

        var graph = new List<string> { $"Business: {business.Name}" };
        if (services.Count > 0) graph.Add("Services: " + string.Join(", ", services));
        if (projects.Count > 0) graph.Add("Projects: " + string.Join(", ", projects));
        foreach (var fact in facts) graph.Add($"Approved fact {fact.FactTypeCode}: {fact.Value}");

        var completion = await _ai.CompleteAsync(
            new AiCompletionRequest("content", prompt, evidence, graph),
            cancellationToken);

        var assembled = ContentComposer.AssembleFromEvidence(business.Name, prompt, facts.Select(f => $"{f.FactTypeCode}: {f.Value}"), services, projects);
        var body = completion.IsLive && !string.IsNullOrWhiteSpace(completion.Output) ? completion.Output : assembled;
        var excerpt = body.Length <= 160 ? body : body[..160];
        ContentItem item;
        try
        {
            item = ContentItem.Draft(
                tenantId,
                businessId,
                "ARTICLE",
                prompt.Length <= 160 ? prompt : prompt[..160],
                null,
                excerpt,
                body,
                ContentVisibility.Private,
                null,
                _user.IsAuthenticated ? _user.UserId : null,
                null,
                null);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        item.MarkHold(completion.IsLive
            ? "Generated from retrieved Graphify context and approved facts. Review before approval."
            : "Assembled from stored identity, services, projects, and approved facts. No live model was called.");

        await ContentComposer.EnsureUniqueSlugAsync(_db, businessId, item.Slug, null, cancellationToken);
        await ContentComposer.AssignTypeAsync(_db, item, item.ContentTypeCode, cancellationToken);
        _db.ContentItems.Add(item);
        await ContentComposer.AnalyzeAndStoreAsync(_db, tenantId, item, null, cancellationToken);
        ContentComposer.Revise(_db, tenantId, item, 1, completion.IsLive ? "AI draft" : "Assembled draft", _user.IsAuthenticated ? _user.UserId : null);
        await ContentComposer.IndexAsync(_db, _search, tenantId, item, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return (await ContentComposer.LoadAsync(_db, businessId, item.Id, cancellationToken))!;
    }
}

public sealed class RejectHubContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public RejectHubContentHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<HubContentResponse> Handle(Guid businessId, Guid contentId, RejectHubContentRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.ContentItems.FirstOrDefaultAsync(c => c.Id == contentId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Content was not found.");
        if (item.Status is ContentItemStatus.Published or ContentItemStatus.Archived)
        {
            throw AppException.Validation("Published or archived content cannot be rejected. Archive it instead.");
        }

        var note = string.IsNullOrWhiteSpace(request.Note) ? "Rejected on the Content Hub desk." : request.Note.Trim();
        item.MarkRejected(note);
        var approval = ApprovalRequest.OpenFor(tenantId, item.Id, "Content Hub rejection.");
        _db.ApprovalRequests.Add(approval);
        _db.ApprovalDecisions.Add(ApprovalDecision.Record(tenantId, approval.Id, ApprovalDecisionKind.Rejected, note));
        approval.Close();
        await _db.SaveChangesAsync(cancellationToken);
        return (await ContentComposer.LoadAsync(_db, businessId, item.Id, cancellationToken))!;
    }
}

public sealed class RestoreHubRevisionHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _user;
    private readonly ISearchProvider _search;

    public RestoreHubRevisionHandler(IAppDbContext db, ITenantContext tenant, ICurrentUser user, ISearchProvider search)
    {
        _db = db;
        _tenant = tenant;
        _user = user;
        _search = search;
    }

    public async Task<HubContentResponse> Handle(Guid businessId, Guid contentId, Guid revisionId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.ContentItems.FirstOrDefaultAsync(c => c.Id == contentId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Content was not found.");
        var revision = await _db.ContentRevisions.FirstOrDefaultAsync(r => r.Id == revisionId && r.ContentItemId == item.Id, cancellationToken)
            ?? throw AppException.NotFound("Revision was not found.");
        try
        {
            item.UpdateDraft(
                item.ContentTypeCode,
                revision.Title,
                item.Slug,
                revision.Excerpt,
                revision.Body,
                item.Visibility,
                item.ProjectId,
                item.FeaturedMediaAssetId,
                item.CanonicalUrl);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        var version = await _db.ContentRevisions.CountAsync(r => r.ContentItemId == item.Id, cancellationToken) + 1;
        ContentComposer.Revise(_db, tenantId, item, version, $"Restored version {revision.VersionNumber}", _user.IsAuthenticated ? _user.UserId : null);
        await ContentComposer.AnalyzeAndStoreAsync(_db, tenantId, item, null, cancellationToken);
        await ContentComposer.IndexAsync(_db, _search, tenantId, item, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return (await ContentComposer.LoadAsync(_db, businessId, item.Id, cancellationToken))!;
    }
}

public sealed class AttachHubMediaHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public AttachHubMediaHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<HubContentResponse> Handle(Guid businessId, Guid contentId, AttachHubMediaRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.ContentItems.FirstOrDefaultAsync(c => c.Id == contentId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Content was not found.");
        var asset = await _db.MediaAssets.FirstOrDefaultAsync(m => m.Id == request.MediaAssetId && m.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Media was not found on this business.");
        if (!Enum.TryParse<ContentMediaRole>(request.Role, true, out var role))
        {
            throw AppException.Validation("Role must be Featured, Body, Gallery, Thumbnail, or Social.");
        }

        var exists = await _db.ContentItemMedia.AnyAsync(
            m => m.ContentItemId == item.Id && m.MediaAssetId == asset.Id && m.Role == role,
            cancellationToken);
        if (exists)
        {
            throw AppException.Conflict("That media is already attached with this role.");
        }

        var order = await _db.ContentItemMedia.CountAsync(m => m.ContentItemId == item.Id, cancellationToken);
        _db.ContentItemMedia.Add(ContentItemMedia.Attach(tenantId, item.Id, asset.Id, role, order));
        if (role == ContentMediaRole.Featured)
        {
            item.SetFeaturedMedia(asset.Id);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (await ContentComposer.LoadAsync(_db, businessId, item.Id, cancellationToken))!;
    }
}

public sealed class RegisterHubMediaHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public RegisterHubMediaHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<HubMediaAssetResponse> Handle(Guid businessId, RegisterHubMediaRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        if (!SafeUrlPolicy.TryValidate(request.SourceUrl, out var uri, out var error) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw AppException.Validation(string.IsNullOrWhiteSpace(error) ? "Featured image URL must be https and a public host." : error);
        }

        if (!Enum.TryParse<MediaKind>(request.Kind, true, out var kind))
        {
            kind = MediaKind.Image;
        }

        var asset = MediaAsset.Register(tenantId, businessId, request.Label, kind, uri.AbsoluteUri);
        _db.MediaAssets.Add(asset);
        await _db.SaveChangesAsync(cancellationToken);
        return new HubMediaAssetResponse(asset.Id, asset.Label, asset.Kind.ToString(), asset.SourceUrl);
    }
}

public sealed class CancelHubScheduleHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public CancelHubScheduleHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<HubContentResponse> Handle(Guid businessId, Guid contentId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.ContentItems.FirstOrDefaultAsync(c => c.Id == contentId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Content was not found.");
        try
        {
            item.ClearSchedule();
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        var calendar = await _db.ContentCalendar.Where(c => c.ContentItemId == item.Id && c.Status == "Scheduled").ToListAsync(cancellationToken);
        foreach (var entry in calendar) entry.Cancel();
        await _db.SaveChangesAsync(cancellationToken);
        return (await ContentComposer.LoadAsync(_db, businessId, item.Id, cancellationToken))!;
    }
}

public sealed class GetPublicHubIndexHandler
{
    private readonly IAppDbContext _db;

    public GetPublicHubIndexHandler(IAppDbContext db) => _db = db;

    public async Task<PublicHubIndex> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var business = await _db.Businesses.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken)
            ?? throw AppException.NotFound("Business was not found.");
        var items = await _db.ContentItems.IgnoreQueryFilters().AsNoTracking()
            .Where(c =>
                c.BusinessId == businessId &&
                c.Status == ContentItemStatus.Published &&
                c.Visibility == ContentVisibility.Public)
            .OrderByDescending(c => c.PublishedAtUtc)
            .ToListAsync(cancellationToken);
        var images = await ContentComposer.FeaturedUrlsAsync(_db, items, cancellationToken);
        return new PublicHubIndex(
            business.Id,
            business.Name,
            items.Select(item => new PublicHubArticleSummary(
                item.Title,
                item.Slug,
                item.Excerpt,
                item.ContentTypeCode,
                item.PublishedAtUtc ?? item.UpdatedAtUtc,
                images.GetValueOrDefault(item.Id))).ToList());
    }
}

public sealed class GetPublicHubArticleHandler
{
    private readonly IAppDbContext _db;

    public GetPublicHubArticleHandler(IAppDbContext db) => _db = db;

    public async Task<PublicHubArticle> Handle(Guid businessId, string slug, CancellationToken cancellationToken)
    {
        var business = await _db.Businesses.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken)
            ?? throw AppException.NotFound("Business was not found.");
        var item = await _db.ContentItems.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.BusinessId == businessId &&
                     c.Slug == slug &&
                     c.Status == ContentItemStatus.Published &&
                     c.Visibility == ContentVisibility.Public,
                cancellationToken)
            ?? throw AppException.NotFound("Published article was not found.");
        var images = await ContentComposer.FeaturedUrlsAsync(_db, [item], cancellationToken);
        return new PublicHubArticle(business.Name, item.Title, item.Slug, item.Excerpt, item.Body, item.ContentTypeCode, item.PublishedAtUtc ?? item.UpdatedAtUtc, images.GetValueOrDefault(item.Id));
    }
}

internal static class ContentComposer
{
    public static async Task EnsureFeaturedAsync(
        IAppDbContext db,
        Guid tenantId,
        Guid businessId,
        ContentItem item,
        Guid? mediaAssetId,
        CancellationToken cancellationToken)
    {
        if (mediaAssetId is null)
        {
            return;
        }

        var asset = await db.MediaAssets.FirstOrDefaultAsync(m => m.Id == mediaAssetId && m.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Media was not found on this business.");
        item.SetFeaturedMedia(asset.Id);
        var exists = await db.ContentItemMedia.AnyAsync(
            m => m.ContentItemId == item.Id && m.MediaAssetId == asset.Id && m.Role == ContentMediaRole.Featured,
            cancellationToken);
        if (!exists)
        {
            var order = await db.ContentItemMedia.CountAsync(m => m.ContentItemId == item.Id, cancellationToken);
            db.ContentItemMedia.Add(ContentItemMedia.Attach(tenantId, item.Id, asset.Id, ContentMediaRole.Featured, order));
        }
    }

    public static async Task<Dictionary<Guid, string?>> FeaturedUrlsAsync(
        IAppDbContext db,
        IReadOnlyList<ContentItem> items,
        CancellationToken cancellationToken)
    {
        var ids = items.Select(i => i.FeaturedMediaAssetId).OfType<Guid>().Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var assets = await db.MediaAssets.IgnoreQueryFilters().AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .ToListAsync(cancellationToken);
        return items.ToDictionary(
            item => item.Id,
            item => assets.FirstOrDefault(a => a.Id == item.FeaturedMediaAssetId)?.SourceUrl);
    }

    public static async Task AssignTypeAsync(IAppDbContext db, ContentItem item, string contentTypeCode, CancellationToken cancellationToken)
    {
        var code = contentTypeCode.Trim().ToUpperInvariant();
        var type = await db.ContentTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Code == code, cancellationToken);
        item.AssignType(type?.Id, code);
    }

    public static async Task EnsureUniqueSlugAsync(IAppDbContext db, Guid businessId, string slug, Guid? exceptId, CancellationToken cancellationToken)
    {
        var clash = await db.ContentItems.AnyAsync(
            c => c.BusinessId == businessId && c.Slug == slug && (exceptId == null || c.Id != exceptId),
            cancellationToken);
        if (clash) throw AppException.Conflict("That slug is already used on this business.");
    }

    public static async Task AnalyzeAndStoreAsync(IAppDbContext db, Guid tenantId, ContentItem item, string? focusKeyword, CancellationToken cancellationToken)
    {
        var result = ContentSeo.Evaluate(item.Title, item.Excerpt, item.Body, focusKeyword, item.CanonicalUrl, item.Slug);
        var captured = ContentSeoAnalysis.Capture(
            tenantId,
            item.Id,
            focusKeyword,
            result.SearchIntent,
            result.Passed,
            result.Total,
            result.MetaTitle,
            result.MetaDescription,
            item.CanonicalUrl,
            ContentSeo.NotesJson(result.Notes),
            result.ReadabilityScore,
            result.AeoScore,
            result.SlugScore,
            result.InternalLinkScore,
            result.EntityCoverageScore);
        var existing = db.ContentSeoAnalyses.Local.FirstOrDefault(s => s.ContentItemId == item.Id)
            ?? await db.ContentSeoAnalyses.FirstOrDefaultAsync(s => s.ContentItemId == item.Id, cancellationToken);
        if (existing is null)
        {
            db.ContentSeoAnalyses.Add(captured);
        }
        else
        {
            existing.Replace(captured);
        }
    }

    public static void Revise(IAppDbContext db, Guid tenantId, ContentItem item, int version, string summary, Guid? userId) =>
        db.ContentRevisions.Add(ContentRevision.Capture(tenantId, item.Id, version, item.Title, item.Excerpt, item.Body, summary, userId));

    public static async Task SyncTaxonomyAsync(
        IAppDbContext db,
        Guid tenantId,
        Guid businessId,
        Guid contentId,
        IReadOnlyList<string>? categories,
        IReadOnlyList<string>? tags,
        CancellationToken cancellationToken)
    {
        var existingCats = await db.ContentItemCategories.Where(x => x.ContentItemId == contentId).ToListAsync(cancellationToken);
        foreach (var row in existingCats) db.ContentItemCategories.Remove(row);
        var existingTags = await db.ContentItemTags.Where(x => x.ContentItemId == contentId).ToListAsync(cancellationToken);
        foreach (var row in existingTags) db.ContentItemTags.Remove(row);

        foreach (var name in (categories ?? []).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var slug = ContentSlug.From(null, name);
            var category = await db.ContentCategories.FirstOrDefaultAsync(c => c.BusinessId == businessId && c.Slug == slug, cancellationToken);
            if (category is null)
            {
                category = ContentCategory.Create(tenantId, businessId, name.Trim(), null);
                db.ContentCategories.Add(category);
            }

            db.ContentItemCategories.Add(ContentItemCategory.Link(tenantId, contentId, category.Id));
        }

        foreach (var name in (tags ?? []).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var slug = ContentSlug.From(null, name);
            var tag = await db.ContentTags.FirstOrDefaultAsync(t => t.BusinessId == businessId && t.Slug == slug, cancellationToken);
            if (tag is null)
            {
                tag = ContentTag.Create(tenantId, businessId, name.Trim());
                db.ContentTags.Add(tag);
            }

            db.ContentItemTags.Add(ContentItemTag.Link(tenantId, contentId, tag.Id));
        }
    }

    public static async Task OfferAsync(
        IAppDbContext db,
        Guid tenantId,
        Guid businessId,
        string topic,
        string description,
        ContentOpportunitySource source,
        IReadOnlyList<string> titles,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(topic)) return;
        ContentGuard.Require(topic, description);
        var existing = await db.ContentTopics.FirstOrDefaultAsync(t => t.BusinessId == businessId && t.Topic == topic.Trim(), cancellationToken);
        if (existing is null)
        {
            existing = ContentTopic.Create(tenantId, businessId, topic, description, "Informational");
            db.ContentTopics.Add(existing);
        }

        if (await db.ContentOpportunities.AnyAsync(o => o.ContentTopicId == existing.Id && o.Status != ContentOpportunityStatus.Dismissed, cancellationToken))
        {
            return;
        }

        var covered = titles.Any(title => title.Contains(topic.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? topic, StringComparison.OrdinalIgnoreCase));
        db.ContentOpportunities.Add(ContentOpportunity.Open(tenantId, businessId, existing.Id, source, covered ? 100 : 0, $"Suggested from a stored {source.ToString().ToLowerInvariant()} record. Scores are coverage of existing titles, not invented search volume."));
    }

    public static string AssembleFromEvidence(
        string business,
        string prompt,
        IEnumerable<string> facts,
        IReadOnlyList<string> services,
        IReadOnlyList<string> projects)
    {
        var lines = new List<string>
        {
            $"# {prompt}",
            "",
            $"This draft is assembled from stored records for {business}. It is not a live model rewrite.",
            ""
        };
        if (facts.Any())
        {
            lines.Add("## Approved facts");
            lines.AddRange(facts.Select(fact => $"- {fact}"));
            lines.Add("");
        }

        if (services.Count > 0)
        {
            lines.Add("## Services on record");
            lines.AddRange(services.Select(service => $"- {service}"));
            lines.Add("");
        }

        if (projects.Count > 0)
        {
            lines.Add("## Projects on record");
            lines.AddRange(projects.Select(project => $"- {project}"));
        }

        if (lines.Count <= 4)
        {
            lines.Add("No approved facts, services, or projects were available. DigitalPulse will not invent them.");
        }

        return string.Join('\n', lines);
    }

    public static string SearchUrl(ContentItem item) =>
        ContentHubPaths.SearchUrl(item.BusinessId, item.Id, item.Slug, item.Status, item.Visibility);

    public static async Task IndexAsync(IAppDbContext db, ISearchProvider search, Guid tenantId, ContentItem item, CancellationToken cancellationToken)
    {
        await GraphifySync.AttachContentAsync(db, tenantId, item, cancellationToken);
        await search.IndexAsync(
            new SearchDocument(tenantId, item.BusinessId, "content", item.Title, item.Body, SearchUrl(item)),
            cancellationToken);
    }

    public static async Task<ContentHubWorkspace> WorkspaceAsync(IAppDbContext db, Guid businessId, CancellationToken cancellationToken)
    {
        var items = await db.ContentItems.AsNoTracking()
            .Where(c => c.BusinessId == businessId && c.ProjectId == null)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
        var seo = await db.ContentSeoAnalyses.AsNoTracking()
            .Where(s => items.Select(i => i.Id).Contains(s.ContentItemId))
            .ToListAsync(cancellationToken);
        var opportunities = await db.ContentOpportunities.AsNoTracking()
            .Where(o => o.BusinessId == businessId && o.Status != ContentOpportunityStatus.Dismissed)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var topics = await db.ContentTopics.AsNoTracking()
            .Where(t => opportunities.Select(o => o.ContentTopicId).Contains(t.Id))
            .ToListAsync(cancellationToken);
        var calendar = await db.ContentCalendar.AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .OrderBy(c => c.ScheduledAtUtc)
            .ToListAsync(cancellationToken);
        var categories = await db.ContentCategories.AsNoTracking().Where(c => c.BusinessId == businessId).ToListAsync(cancellationToken);
        var tags = await db.ContentTags.AsNoTracking().Where(t => t.BusinessId == businessId).ToListAsync(cancellationToken);
        var media = await db.MediaAssets.AsNoTracking()
            .Where(m => m.BusinessId == businessId)
            .OrderByDescending(m => m.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        return new ContentHubWorkspace(
            ContentTypeCatalog.All.Select(t => new ContentTypeResponse(t.Code, t.Name)).ToList(),
            items.Select(item => item.ToSummary(seo.LastOrDefault(s => s.ContentItemId == item.Id))).ToList(),
            opportunities.Select(o =>
            {
                var topic = topics.FirstOrDefault(t => t.Id == o.ContentTopicId);
                return new ContentOpportunityResponse(o.Id, o.ContentTopicId, topic?.Topic ?? "Topic", topic?.Description ?? "", o.SourceType.ToString(), o.CoverageScore, o.OpportunityScore, o.RelevanceScore, o.CompetitionScore, o.Priority, o.Reason, o.Status.ToString());
            }).ToList(),
            calendar.Select(c =>
            {
                var item = items.FirstOrDefault(i => i.Id == c.ContentItemId);
                return new ContentCalendarResponse(c.Id, c.ContentItemId, item?.Title ?? "Scheduled", c.ScheduledAtUtc, c.Status, c.Channel);
            }).ToList(),
            categories.Select(c => new ContentNamedResponse(c.Id, c.Name, c.Slug)).ToList(),
            tags.Select(t => new ContentNamedResponse(t.Id, t.Name, t.Slug)).ToList(),
            await MetricsAsync(db, businessId, null, cancellationToken),
            media.Select(m => new HubMediaAssetResponse(m.Id, m.Label, m.Kind.ToString(), m.SourceUrl)).ToList(),
            "SEO score is checks passed ÷ checks run. Opportunity scores are coverage of existing titles. Analytics stay empty until an official provider returns them.");
    }

    public static async Task<HubContentResponse?> LoadAsync(IAppDbContext db, Guid businessId, Guid contentId, CancellationToken cancellationToken)
    {
        var item = await db.ContentItems.AsNoTracking().FirstOrDefaultAsync(c => c.Id == contentId && c.BusinessId == businessId, cancellationToken);
        if (item is null) return null;
        var seo = await db.ContentSeoAnalyses.AsNoTracking().Where(s => s.ContentItemId == item.Id).OrderByDescending(s => s.LastAnalyzedAtUtc).FirstOrDefaultAsync(cancellationToken);
        var revisions = await db.ContentRevisions.AsNoTracking().Where(r => r.ContentItemId == item.Id).OrderByDescending(r => r.VersionNumber).ToListAsync(cancellationToken);
        var variants = await db.ContentVariants.AsNoTracking().Where(v => v.ContentItemId == item.Id).ToListAsync(cancellationToken);
        var distributions = await db.ContentDistributions.AsNoTracking().Where(d => d.ContentItemId == item.Id).ToListAsync(cancellationToken);
        var catIds = await db.ContentItemCategories.AsNoTracking().Where(x => x.ContentItemId == item.Id).Select(x => x.ContentCategoryId).ToListAsync(cancellationToken);
        var tagIds = await db.ContentItemTags.AsNoTracking().Where(x => x.ContentItemId == item.Id).Select(x => x.ContentTagId).ToListAsync(cancellationToken);
        var cats = await db.ContentCategories.AsNoTracking().Where(c => catIds.Contains(c.Id)).Select(c => c.Name).ToListAsync(cancellationToken);
        var tags = await db.ContentTags.AsNoTracking().Where(t => tagIds.Contains(t.Id)).Select(t => t.Name).ToListAsync(cancellationToken);
        var mediaRows = await db.ContentItemMedia.AsNoTracking().Where(m => m.ContentItemId == item.Id).OrderBy(m => m.DisplayOrder).ToListAsync(cancellationToken);
        var mediaIds = mediaRows.Select(m => m.MediaAssetId).ToList();
        var assets = await db.MediaAssets.AsNoTracking().Where(a => mediaIds.Contains(a.Id)).ToListAsync(cancellationToken);
        var seoResponse = seo is null
            ? new ContentSeoResponse("Unknown", 0, 8, 0, item.Title, item.Excerpt, null, item.CanonicalUrl, ["Analyze to run the checklist."], null, 0, 0, 0, 0, 0)
            : new ContentSeoResponse(seo.SearchIntent, seo.ChecksPassed, seo.ChecksTotal, seo.SeoScore, seo.MetaTitle, seo.MetaDescription, seo.FocusKeyword, seo.CanonicalUrl, ContentSeo.ReadNotes(seo.NotesJson), seo.LastAnalyzedAtUtc, seo.ReadabilityScore, seo.AeoScore, seo.SlugScore, seo.InternalLinkScore, seo.EntityCoverageScore);

        return new HubContentResponse(
            item.Id,
            item.BusinessId,
            item.ProjectId,
            item.ContentTypeCode,
            item.Title,
            item.Slug,
            item.Excerpt,
            item.Body,
            item.Status.ToString(),
            item.Visibility.ToString(),
            item.SourceNote,
            item.FeaturedMediaAssetId,
            item.CanonicalUrl,
            item.PublishedAtUtc,
            item.ScheduledAtUtc,
            item.UpdatedAtUtc,
            cats,
            tags,
            seoResponse,
            revisions.Select(r => new ContentRevisionResponse(r.Id, r.VersionNumber, r.Title, r.ChangeSummary, r.CreatedAtUtc)).ToList(),
            variants.Select(v => new ContentVariantHubResponse(v.Id, v.Kind.ToString(), v.Title, v.Body, v.Status.ToString(), v.PublicationHold)).ToList(),
            distributions.Select(d => new ContentDistributionResponse(d.Id, d.ProviderCode, d.Status.ToString(), d.FailureReason, d.PublishedAtUtc)).ToList(),
            await MetricsAsync(db, businessId, item.Id, cancellationToken),
            mediaRows.Select(m =>
            {
                var asset = assets.FirstOrDefault(a => a.Id == m.MediaAssetId);
                return new ContentMediaResponse(m.Id, m.MediaAssetId, m.Role.ToString(), m.DisplayOrder, asset?.Label, asset?.SourceUrl);
            }).ToList());
    }

    public static string ExcerptOf(string? excerpt, string body)
    {
        if (!string.IsNullOrWhiteSpace(excerpt)) return excerpt.Trim();
        var text = body.Trim();
        return text.Length <= 160 ? text : text[..160];
    }

    private static async Task<IReadOnlyList<ContentMetricResponse>> MetricsAsync(IAppDbContext db, Guid businessId, Guid? contentId, CancellationToken cancellationToken)
    {
        var rows = await db.ContentMetrics.AsNoTracking()
            .Where(m => m.BusinessId == businessId && (contentId == null || m.ContentItemId == contentId))
            .OrderByDescending(m => m.MetricDate)
            .Take(40)
            .ToListAsync(cancellationToken);
        return rows.Select(m => new ContentMetricResponse(m.ProviderCode, m.MetricDate, m.Views, m.Detail)).ToList();
    }
}
