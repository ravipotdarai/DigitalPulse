using System.Net;
using System.Text.RegularExpressions;

namespace DigitalPulse.Application.Website;

public sealed record HtmlSignals(
    string? Title,
    string? MetaDescription,
    string? H1,
    string? Canonical,
    string? Robots,
    bool HasJsonLd,
    bool HasFaqSchema,
    bool HasOrganizationSchema,
    bool HasOgTitle,
    IReadOnlyList<string> QuestionHeadings,
    string Text,
    int WordCount);

public static class HtmlSignalParser
{
    private static readonly Regex TitleRegex = new(@"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
    private static readonly Regex MetaRegex = new(@"<meta\s+[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
    private static readonly Regex H1Regex = new(@"<h1[^>]*>(.*?)</h1>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
    private static readonly Regex HeadingRegex = new(@"<h[1-3][^>]*>(.*?)</h[1-3]>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
    private static readonly Regex CanonicalRegex = new(@"<link[^>]*rel\s*=\s*[""']canonical[""'][^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
    private static readonly Regex JsonLdRegex = new(@"<script[^>]*type\s*=\s*[""']application/ld\+json[""'][^>]*>(.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
    private static readonly Regex TagRegex = new(@"<script\b[^>]*>.*?</script>|<style\b[^>]*>.*?</style>|<[^>]+>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromMilliseconds(400));
    private static readonly Regex AttrRegex = new(@"(?<name>[a-zA-Z0-9:-]+)\s*=\s*[""'](?<value>[^""']*)[""']", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));

    public static HtmlSignals Parse(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return new HtmlSignals(null, null, null, null, null, false, false, false, false, [], string.Empty, 0);
        }

        var title = Decode(FirstGroup(TitleRegex, html));
        var meta = ReadMeta(html);
        var h1 = Decode(StripTags(FirstGroup(H1Regex, html)));
        var canonical = ReadCanonical(html);
        var jsonLd = string.Join('\n', JsonLdRegex.Matches(html).Select(m => m.Groups[1].Value));
        var hasJsonLd = jsonLd.Length > 0;
        var text = Decode(TagRegex.Replace(html, " ")) ?? string.Empty;
        text = Regex.Replace(text, @"\s+", " ", RegexOptions.None, TimeSpan.FromMilliseconds(200)).Trim();
        var words = text.Length == 0 ? 0 : text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var questions = HeadingRegex.Matches(html)
            .Select(m => Decode(StripTags(m.Groups[1].Value)) ?? string.Empty)
            .Where(h => h.Contains('?', StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();

        return new HtmlSignals(
            title,
            meta.Description,
            h1,
            canonical,
            meta.Robots,
            hasJsonLd,
            ContainsType(jsonLd, "FAQPage"),
            ContainsType(jsonLd, "Organization") || ContainsType(jsonLd, "LocalBusiness"),
            !string.IsNullOrWhiteSpace(meta.OgTitle),
            questions,
            text.Length > 4000 ? text[..4000] : text,
            words);
    }

    public static bool ContainsName(string? haystack, string? name) =>
        !string.IsNullOrWhiteSpace(haystack)
        && !string.IsNullOrWhiteSpace(name)
        && haystack.Contains(name.Trim(), StringComparison.OrdinalIgnoreCase);

    public static bool ContainsPhone(string? haystack, string? phone)
    {
        if (string.IsNullOrWhiteSpace(haystack) || string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }

        var expected = new string(phone.Where(char.IsDigit).ToArray());
        return expected.Length >= 8 && new string(haystack.Where(char.IsDigit).ToArray()).Contains(expected, StringComparison.Ordinal);
    }

    private static (string? Description, string? Robots, string? OgTitle) ReadMeta(string html)
    {
        string? description = null;
        string? robots = null;
        string? ogTitle = null;
        foreach (Match match in MetaRegex.Matches(html))
        {
            var attrs = ReadAttrs(match.Value);
            var name = attrs.GetValueOrDefault("name") ?? attrs.GetValueOrDefault("property");
            if (string.IsNullOrWhiteSpace(name) || !attrs.TryGetValue("content", out var content))
            {
                continue;
            }

            if (name.Equals("description", StringComparison.OrdinalIgnoreCase))
            {
                description = Decode(content);
            }
            else if (name.Equals("robots", StringComparison.OrdinalIgnoreCase))
            {
                robots = Decode(content);
            }
            else if (name.Equals("og:title", StringComparison.OrdinalIgnoreCase))
            {
                ogTitle = Decode(content);
            }
        }

        return (description, robots, ogTitle);
    }

    private static string? ReadCanonical(string html)
    {
        var match = CanonicalRegex.Match(html);
        if (!match.Success)
        {
            return null;
        }

        var attrs = ReadAttrs(match.Value);
        return attrs.TryGetValue("href", out var href) ? Decode(href) : null;
    }

    private static Dictionary<string, string> ReadAttrs(string tag)
    {
        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in AttrRegex.Matches(tag))
        {
            attrs[match.Groups["name"].Value] = match.Groups["value"].Value;
        }

        return attrs;
    }

    private static bool ContainsType(string jsonLd, string type) =>
        jsonLd.Contains($"\"{type}\"", StringComparison.OrdinalIgnoreCase)
        || jsonLd.Contains($"'{type}'", StringComparison.OrdinalIgnoreCase);

    private static string? FirstGroup(Regex regex, string html)
    {
        var match = regex.Match(html);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? StripTags(string? value) =>
        string.IsNullOrWhiteSpace(value) ? value : Regex.Replace(value, "<[^>]+>", string.Empty, RegexOptions.None, TimeSpan.FromMilliseconds(100));

    private static string? Decode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : WebUtility.HtmlDecode(value).Trim();
}
