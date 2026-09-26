using System.Text.RegularExpressions;
using DigitalPulse.Domain.Common;
using DigitalPulse.Domain.Projects;

namespace DigitalPulse.Domain.Content;

public sealed class ContentType : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    private ContentType() { }

    public static ContentType Create(string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ContentType
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim()
        };
    }
}

public static class ContentTypeCatalog
{
    public static IReadOnlyList<(string Code, string Name)> All { get; } =
    [
        ("ARTICLE", "Article"),
        ("CASE_STUDY", "Case study"),
        ("GUIDE", "Guide"),
        ("HOW_TO", "How-to"),
        ("NEWS", "News"),
        ("ANNOUNCEMENT", "Announcement"),
        ("FAQ", "FAQ"),
        ("CUSTOMER_STORY", "Customer story"),
        ("SERVICE_GUIDE", "Service guide"),
        ("COMPARISON", "Comparison"),
        ("LOCAL_GUIDE", "Local guide"),
        ("INDUSTRY_INSIGHT", "Industry insight"),
        ("PROJECT_STORY", "Project story")
    ];

    public static bool Exists(string code) =>
        All.Any(item => item.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
}

public static class ContentHubPaths
{
    public static string SearchUrl(Guid businessId, Guid contentId, string slug, ContentItemStatus status, ContentVisibility visibility) =>
        status == ContentItemStatus.Published && visibility == ContentVisibility.Public
            ? $"/hub/{businessId:D}/{slug}"
            : $"hub://{contentId:N}";

    public static string PublicMedia(Guid businessId, Guid assetId) =>
        $"/v1/hub/{businessId:D}/media/{assetId:D}";

    public static string? DisplayMedia(Guid businessId, Guid? assetId, string? sourceUrl)
    {
        if (assetId is null) return null;
        var value = sourceUrl?.Trim();
        if (string.IsNullOrWhiteSpace(value)) return PublicMedia(businessId, assetId.Value);
        if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/v1/hub/", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/hub/", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        return PublicMedia(businessId, assetId.Value);
    }
}

public static class ContentSlug
{
    public static string From(string? slug, string title)
    {
        var source = string.IsNullOrWhiteSpace(slug) ? title : slug;
        var folded = source.Trim().ToLowerInvariant();
        var cleaned = Regex.Replace(folded, @"[^a-z0-9]+", "-").Trim('-');
        if (cleaned.Length == 0) cleaned = "content";
        if (cleaned.Length > 80) cleaned = cleaned[..80].Trim('-');
        return cleaned;
    }
}
