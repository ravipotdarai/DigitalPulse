using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Ai;
using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Identity;
using DigitalPulse.Contracts.Social;
using DigitalPulse.Domain.Platforms;
using DigitalPulse.Domain.Social;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Social;

internal static class SocialChannels
{
    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "GOOGLE", "FACEBOOK", "INSTAGRAM", "LINKEDIN", "YOUTUBE"
    };

    public static SocialContentKind KindFor(string platformCode) => platformCode.ToUpperInvariant() switch
    {
        "GOOGLE" => SocialContentKind.GooglePost,
        "FACEBOOK" => SocialContentKind.FacebookPost,
        "INSTAGRAM" => SocialContentKind.InstagramCaption,
        "LINKEDIN" => SocialContentKind.LinkedInPost,
        "YOUTUBE" => SocialContentKind.YouTubeMetadata,
        _ => throw AppException.Validation("WhatsApp and directory platforms are not social posts in this phase.")
    };

    public static SocialContentResponse ToResponse(this SocialContentItem item, bool analyticsLive = false) =>
        new(
            item.Id,
            item.BusinessId,
            item.PlatformCode,
            item.Kind.ToString(),
            item.Title,
            item.Body,
            item.Status.ToString(),
            item.VerificationStatus.ToString(),
            item.VerificationDetail,
            item.LastPublishError,
            item.CreatedAtUtc,
            item.UpdatedAtUtc,
            SocialPostChecks.Evaluate(item.Title, item.Body, analyticsLive));

    public static bool AnalyticsLive(IEnumerable<DigitalPulse.Domain.Platforms.PlatformConnection> connections) =>
        connections.Any(c =>
            c.PlatformCode.Equals("GOOGLE_ANALYTICS", StringComparison.OrdinalIgnoreCase) && c.HasLiveCredential);

    public static async Task<bool> AnalyticsLiveAsync(IAppDbContext db, Guid businessId, CancellationToken cancellationToken)
    {
        var links = await db.Connections.AsNoTracking()
            .Where(c => c.BusinessId == businessId && c.PlatformCode == "GOOGLE_ANALYTICS")
            .ToListAsync(cancellationToken);
        return AnalyticsLive(links);
    }
}

public sealed class GetSocialWorkspaceHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPlatformAdapterCatalog _catalog;

    public GetSocialWorkspaceHandler(IAppDbContext db, ITenantContext tenant, IPlatformAdapterCatalog catalog)
    {
        _db = db;
        _tenant = tenant;
        _catalog = catalog;
    }

    public async Task<SocialWorkspaceResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var connections = await _db.Connections.AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .ToListAsync(cancellationToken);
        var metrics = (await _db.SocialMetrics.AsNoTracking()
            .Where(m => m.BusinessId == businessId)
            .ToListAsync(cancellationToken))
            .GroupBy(m => m.PlatformCode)
            .Select(g => g.OrderByDescending(m => m.CapturedAtUtc).First())
            .ToList();
        var items = await _db.SocialContent.AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        var channels = _catalog.All()
            .Where(a => SocialChannels.Allowed.Contains(a.Describe().Code))
            .Select(adapter =>
            {
                var d = adapter.Describe();
                var link = connections.FirstOrDefault(c => c.PlatformCode.Equals(d.Code, StringComparison.OrdinalIgnoreCase));
                var metric = metrics.FirstOrDefault(m => m.PlatformCode.Equals(d.Code, StringComparison.OrdinalIgnoreCase));
                return new SocialChannelResponse(
                    d.Code,
                    d.Name,
                    d.Category,
                    d.Capabilities.CanPublish,
                    d.Capabilities.CanGetMetrics,
                    d.Capabilities.AssistedOnly,
                    link?.Status.ToString(),
                    link?.GrantKind,
                    metric?.Status.ToString(),
                    metric?.Detail);
            })
            .ToList();

        return new SocialWorkspaceResponse(
            channels,
            items.Select(i => i.ToResponse(SocialChannels.AnalyticsLive(connections))).ToList(),
            "After you sign in on the official platform and approve, DigitalPulse posts the draft — including the attached image or short video — through the official API. WhatsApp is not a social post.");
    }

}

public sealed class CreateSocialContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPlatformAdapterCatalog _catalog;

    public CreateSocialContentHandler(IAppDbContext db, ITenantContext tenant, IPlatformAdapterCatalog catalog)
    {
        _db = db;
        _tenant = tenant;
        _catalog = catalog;
    }

    public async Task<SocialContentResponse> Handle(Guid businessId, CreateSocialContentRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.PlatformCode) || !SocialChannels.Allowed.Contains(request.PlatformCode))
        {
            throw AppException.Validation("Choose Google, Facebook, Instagram, LinkedIn, or YouTube. WhatsApp is not a social post.");
        }

        _catalog.Get(request.PlatformCode);
        SocialContentItem item;
        try
        {
            item = SocialContentItem.Draft(
                tenantId,
                businessId,
                request.PlatformCode,
                SocialChannels.KindFor(request.PlatformCode),
                request.Title,
                request.Body);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        _db.SocialContent.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return item.ToResponse(await SocialChannels.AnalyticsLiveAsync(_db, businessId, cancellationToken));
    }
}

public sealed class GenerateSocialDraftHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAiContextBuilder _context;
    private readonly IAiOrchestrator _orchestrator;

    public GenerateSocialDraftHandler(IAppDbContext db, ITenantContext tenant, IAiContextBuilder context, IAiOrchestrator orchestrator)
    {
        _db = db;
        _tenant = tenant;
        _context = context;
        _orchestrator = orchestrator;
    }

    public async Task<SocialContentResponse> Handle(Guid businessId, GenerateSocialDraftRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var platform = string.IsNullOrWhiteSpace(request.PlatformCode) ? "GOOGLE" : request.PlatformCode.Trim();
        if (!SocialChannels.Allowed.Contains(platform))
        {
            throw AppException.Validation("Choose Google, Facebook, Instagram, LinkedIn, or YouTube.");
        }

        if (string.IsNullOrWhiteSpace(request.Prompt) || request.Prompt.Trim().Length < 4)
        {
            throw AppException.Validation("Ask at least four characters so retrieval has something to match.");
        }

        if (AiAgents.Require("social").MayExecuteExternally)
        {
            throw AppException.Validation("AI agents cannot execute platform posts. Use Actions after approval.");
        }

        var built = await _context.BuildAsync(tenantId, businessId, request.Prompt.Trim(), cancellationToken);
        var completion = await _orchestrator.RunAsync(
            new AiOrchestrationRequest("social", request.Prompt.Trim(), built.Evidence, built.GraphLines),
            cancellationToken);
        var title = request.Prompt.Trim().Length <= 80 ? request.Prompt.Trim() : request.Prompt.Trim()[..80];
        var body = string.IsNullOrWhiteSpace(completion.Output)
            ? string.Join('\n', built.Evidence.Where(item => !item.Restricted).Select(item => item.Body).Take(6))
            : completion.Output;
        if (string.IsNullOrWhiteSpace(body))
        {
            body = "No publishable evidence was retrieved. DigitalPulse will not invent a Google post.";
        }

        SocialContentItem item;
        try
        {
            item = SocialContentItem.Draft(tenantId, businessId, platform, SocialChannels.KindFor(platform), title, body);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        item.MarkAssisted(completion.ProviderIsLive
            ? "Assisted social draft from the AI layer. Approval and the official adapter are still required before publish."
            : "Assembled from stored Graphify context. No live model was called. Nothing was posted.");
        _db.SocialContent.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return item.ToResponse(await SocialChannels.AnalyticsLiveAsync(_db, businessId, cancellationToken));
    }
}

public sealed class UpdateSocialContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public UpdateSocialContentHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<SocialContentResponse> Handle(
        Guid businessId,
        Guid contentId,
        UpdateSocialContentRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.SocialContent.FirstOrDefaultAsync(c => c.Id == contentId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Social content was not found.");
        try
        {
            item.UpdateDraft(request.Title, request.Body);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return item.ToResponse(await SocialChannels.AnalyticsLiveAsync(_db, businessId, cancellationToken));
    }
}

public sealed class ApproveSocialContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public ApproveSocialContentHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<SocialContentResponse> Handle(Guid businessId, Guid contentId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.SocialContent.FirstOrDefaultAsync(c => c.Id == contentId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Social content was not found.");
        var safety = DigitalPulse.Domain.Safety.ContentSafety.Assess(item.Title, item.Body);
        if (!safety.Allowed)
        {
            throw AppException.Validation(safety.Detail);
        }

        item.Approve();
        await _db.SaveChangesAsync(cancellationToken);
        return item.ToResponse(await SocialChannels.AnalyticsLiveAsync(_db, businessId, cancellationToken));
    }
}

public sealed class DeleteSocialContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public DeleteSocialContentHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task Handle(Guid businessId, Guid contentId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.SocialContent.FirstOrDefaultAsync(c => c.Id == contentId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Social content was not found.");
        try
        {
            item.Discard();
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        _db.SocialContent.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class PublishSocialContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPlatformAdapterCatalog _catalog;
    private readonly ISocialMediaStore _media;

    public PublishSocialContentHandler(IAppDbContext db, ITenantContext tenant, IPlatformAdapterCatalog catalog, ISocialMediaStore media)
    {
        _db = db;
        _tenant = tenant;
        _catalog = catalog;
        _media = media;
    }

    public async Task<SocialContentResponse> Handle(Guid businessId, Guid contentId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.SocialContent.FirstOrDefaultAsync(c => c.Id == contentId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Social content was not found.");
        if (item.Status != SocialContentStatus.Approved)
        {
            throw AppException.Validation("Approve the draft before publishing.");
        }

        var adapter = _catalog.Get(item.PlatformCode);
        var connection = await _db.Connections.FirstOrDefaultAsync(
            c => c.BusinessId == businessId && c.PlatformCode == item.PlatformCode, cancellationToken);
        if (connection is null)
        {
            item.MarkBlocked("Connect the platform in Connection Center before publish.");
            await _db.SaveChangesAsync(cancellationToken);
            return item.ToResponse();
        }

        var media = await LoadMediaAsync(businessId, item.Body, cancellationToken);
        var result = await adapter.PublishAsync(connection, item.Title, item.Body, media, cancellationToken);
        if (result.Status.Equals("Published", StringComparison.OrdinalIgnoreCase))
        {
            item.MarkPublished(result.Detail);
        }
        else if (result.Status.Equals("Assisted", StringComparison.OrdinalIgnoreCase))
        {
            item.MarkAssisted(result.Detail);
        }
        else if (result.Status.Equals("Hold", StringComparison.OrdinalIgnoreCase) ||
                 result.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase))
        {
            item.MarkBlocked(result.Detail);
        }
        else
        {
            item.MarkFailed(result.Detail);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return item.ToResponse();
    }

    private async Task<PlatformPublishMedia> LoadMediaAsync(Guid businessId, string body, CancellationToken cancellationToken)
    {
        var parts = SocialDraft.Parse(body);
        byte[]? imageBytes = null;
        byte[]? videoBytes = null;
        string? imageName = null;
        string? videoName = null;
        if (parts.ImageFileId is Guid imageId)
        {
            var asset = await _db.MediaAssets.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == imageId && a.BusinessId == businessId, cancellationToken);
            if (asset?.SourceUrl is not null)
            {
                imageBytes = await _media.ReadAsync(asset.SourceUrl, cancellationToken);
                imageName = asset.Label;
            }
        }

        if (parts.VideoFileId is Guid videoId)
        {
            var asset = await _db.MediaAssets.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == videoId && a.BusinessId == businessId, cancellationToken);
            if (asset?.SourceUrl is not null)
            {
                videoBytes = await _media.ReadAsync(asset.SourceUrl, cancellationToken);
                videoName = asset.Label;
            }
        }

        return new PlatformPublishMedia(parts.ImageUrl, parts.VideoUrl, imageName, videoName, imageBytes, videoBytes);
    }
}

public sealed class RefreshSocialMetricsHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPlatformAdapterCatalog _catalog;

    public RefreshSocialMetricsHandler(IAppDbContext db, ITenantContext tenant, IPlatformAdapterCatalog catalog)
    {
        _db = db;
        _tenant = tenant;
        _catalog = catalog;
    }

    public async Task<SocialWorkspaceResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var connections = await _db.Connections
            .Where(c => c.BusinessId == businessId && SocialChannels.Allowed.Contains(c.PlatformCode))
            .ToListAsync(cancellationToken);

        foreach (var code in SocialChannels.Allowed)
        {
            var connection = connections.FirstOrDefault(c => c.PlatformCode.Equals(code, StringComparison.OrdinalIgnoreCase));
            var adapter = _catalog.Get(code);
            if (connection is null)
            {
                _db.SocialMetrics.Add(SocialMetricSnapshot.Hold(
                    tenantId, businessId, null, code, SocialMetricStatus.Unavailable,
                    "No authorized connection. Metrics are not invented."));
                continue;
            }

            var result = await adapter.MetricsAsync(connection, cancellationToken);
            var status = result.Status.Equals("Observed", StringComparison.OrdinalIgnoreCase)
                ? SocialMetricStatus.Observed
                : result.Status.Equals("Hold", StringComparison.OrdinalIgnoreCase)
                    ? SocialMetricStatus.Hold
                    : SocialMetricStatus.Unavailable;
            _db.SocialMetrics.Add(SocialMetricSnapshot.Hold(
                tenantId, businessId, connection.Id, code, status, result.Detail));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await new GetSocialWorkspaceHandler(_db, _tenant, _catalog).Handle(businessId, cancellationToken);
    }
}
