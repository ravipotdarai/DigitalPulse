using DigitalPulse.Application.Abstractions;
using Microsoft.Extensions.Hosting;

namespace DigitalPulse.Infrastructure.Platforms;

public sealed class FileSocialMediaStore(IHostEnvironment environment) : ISocialMediaStore
{
    public async Task<StoredSocialMedia> SaveAsync(
        Guid tenantId,
        Guid id,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(fileName);
        if (ext is { Length: > 8 } || ext.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            ext = contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ? ".mp4" : ".jpg";
        }

        var relative = Path.Combine("App_Data", "social-media", tenantId.ToString("N"), id.ToString("N") + ext);
        var full = Path.Combine(environment.ContentRootPath, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var output = File.Create(full);
        await content.CopyToAsync(output, cancellationToken);
        var kind = contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ? "Video" : "Image";
        return new StoredSocialMedia(id, fileName, contentType, relative.Replace('\\', '/'), kind);
    }

    public async Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;
        var full = Path.Combine(environment.ContentRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(full)) return null;
        return await File.ReadAllBytesAsync(full, cancellationToken);
    }
}
