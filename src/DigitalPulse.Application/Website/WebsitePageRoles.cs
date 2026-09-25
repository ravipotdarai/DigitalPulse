using DigitalPulse.Domain.Website;

namespace DigitalPulse.Application.Website;

public static class WebsitePageRoles
{
    public static WebsitePageRole Classify(string? url, string? title, string? h1)
    {
        var hay = $"{url} {title} {h1}";
        if (LooksLike(hay, "contact", "contact-us", "get-in-touch", "reach-us"))
        {
            return WebsitePageRole.Contact;
        }

        if (LooksLike(hay, "about", "about-us", "our-story", "who-we-are", "vision", "mission"))
        {
            return WebsitePageRole.About;
        }

        if (IsHome(url))
        {
            return WebsitePageRole.Home;
        }

        return WebsitePageRole.Other;
    }

    public static bool IsHome(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return true;
        }

        var path = uri.AbsolutePath.TrimEnd('/');
        return path.Length == 0 || path.Equals("/index", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/index.html", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/home", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLike(string hay, params string[] tokens) =>
        tokens.Any(token => hay.Contains(token, StringComparison.OrdinalIgnoreCase));
}
