namespace DigitalPulse.Application.Abstractions;

public sealed record StoredSocialMedia(Guid Id, string FileName, string ContentType, string RelativePath, string Kind);

public interface ISocialMediaStore
{
    Task<StoredSocialMedia> SaveAsync(
        Guid tenantId,
        Guid id,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken);

    Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken);
}
