using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Identity;
using DigitalPulse.Contracts.Social;
using DigitalPulse.Domain.Projects;

namespace DigitalPulse.Application.Features.Social;

public sealed class UploadSocialMediaHandler
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif",
        "video/mp4", "video/quicktime", "video/webm"
    };

    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ISocialMediaStore _store;

    public UploadSocialMediaHandler(IAppDbContext db, ITenantContext tenant, ISocialMediaStore store)
    {
        _db = db;
        _tenant = tenant;
        _store = store;
    }

    public async Task<SocialMediaResponse> Handle(
        Guid businessId,
        string fileName,
        string contentType,
        long length,
        Stream content,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(contentType) || !Allowed.Contains(contentType))
        {
            throw AppException.Validation("Attach a JPEG, PNG, WebP, GIF, MP4, MOV, or WebM file.");
        }

        var video = contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
        var limit = video ? 32L * 1024 * 1024 : 8L * 1024 * 1024;
        if (length <= 0 || length > limit)
        {
            throw AppException.Validation(video
                ? "Keep the video under 32 MB for this composer."
                : "Keep the image under 8 MB for this composer.");
        }

        var id = Guid.NewGuid();
        var stored = await _store.SaveAsync(tenantId, id, fileName, contentType, content, cancellationToken);
        var kind = video ? MediaKind.Video : MediaKind.Image;
        var asset = MediaAsset.Register(tenantId, businessId, fileName, kind, stored.RelativePath, id);
        _db.MediaAssets.Add(asset);
        await _db.SaveChangesAsync(cancellationToken);
        return new SocialMediaResponse(asset.Id, stored.Kind, stored.FileName, stored.ContentType);
    }
}
