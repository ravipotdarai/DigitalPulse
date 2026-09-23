using DigitalPulse.Application.Abstractions;
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

    public static SocialContentResponse ToResponse(this SocialContentItem item) =>
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
            item.UpdatedAtUtc);
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
            items.Select(i => i.ToResponse()).ToList(),
            "Google, Meta, LinkedIn, and YouTube drafts stay in DigitalPulse until a live provider publish exists. WhatsApp is not a social post.");
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
        var item = SocialContentItem.Draft(
            tenantId,
            businessId,
            request.PlatformCode,
            SocialChannels.KindFor(request.PlatformCode),
            request.Title,
            request.Body);
        _db.SocialContent.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return item.ToResponse();
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
        return item.ToResponse();
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
        item.Approve();
        await _db.SaveChangesAsync(cancellationToken);
        return item.ToResponse();
    }
}

public sealed class PublishSocialContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPlatformAdapterCatalog _catalog;

    public PublishSocialContentHandler(IAppDbContext db, ITenantContext tenant, IPlatformAdapterCatalog catalog)
    {
        _db = db;
        _tenant = tenant;
        _catalog = catalog;
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

        var result = await adapter.PublishAsync(connection, item.Title, item.Body, cancellationToken);
        if (result.Status.Equals("Assisted", StringComparison.OrdinalIgnoreCase))
        {
            item.MarkAssisted(result.Detail);
        }
        else if (result.Status.Equals("Hold", StringComparison.OrdinalIgnoreCase))
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
            var status = result.Status.Equals("Hold", StringComparison.OrdinalIgnoreCase)
                ? SocialMetricStatus.Hold
                : SocialMetricStatus.Unavailable;
            _db.SocialMetrics.Add(SocialMetricSnapshot.Hold(
                tenantId, businessId, connection.Id, code, status, result.Detail));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await new GetSocialWorkspaceHandler(_db, _tenant, _catalog).Handle(businessId, cancellationToken);
    }
}
