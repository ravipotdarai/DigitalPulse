using DigitalPulse.Application.Abstractions;

namespace DigitalPulse.Application.Features.Social;

public sealed record SocialDraftParts(
    string Text,
    string? Location,
    string? ImageUrl,
    string? VideoUrl,
    Guid? ImageFileId,
    Guid? VideoFileId);

public static class SocialDraft
{
    public static SocialDraftParts Parse(string body)
    {
        string? location = null, image = null, video = null, imageFile = null, videoFile = null;
        var text = new List<string>();
        foreach (var raw in (body ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.TrimEnd();
            if (Take(line, "Location:", ref location) ||
                Take(line, "ImageFile:", ref imageFile) ||
                Take(line, "VideoFile:", ref videoFile) ||
                Take(line, "Image:", ref image) ||
                Take(line, "Video:", ref video))
            {
                continue;
            }

            text.Add(line);
        }

        return new SocialDraftParts(
            string.Join("\n", text).Trim(),
            location,
            image,
            video,
            ParseId(imageFile),
            ParseId(videoFile));
    }

    public static PlatformPublishMedia ToMedia(string body, byte[]? imageBytes = null, byte[]? videoBytes = null, string? imageName = null, string? videoName = null)
    {
        var parts = Parse(body);
        return new PlatformPublishMedia(parts.ImageUrl, parts.VideoUrl, imageName, videoName, imageBytes, videoBytes);
    }

    private static bool Take(string line, string prefix, ref string? value)
    {
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        var next = line[prefix.Length..].Trim();
        if (next.Length > 0) value = next;
        return true;
    }

    private static Guid? ParseId(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;
}
