using System.Net;
using System.Text.RegularExpressions;

namespace DigitalPulse.Domain.Content;

public enum ContentBlockKind
{
    Paragraph = 1,
    Heading = 2,
    Quote = 3,
    List = 4,
    Image = 5,
    Video = 6
}

public sealed record ContentBlock(ContentBlockKind Kind, string Text, IReadOnlyList<string> Items, string? Url);

public static class ContentMarkup
{
    private static readonly Regex Image = new(@"^!\[(.*)\]\((.+)\)$", RegexOptions.Compiled);
    private static readonly Regex Video = new(@"^!video\[(.*)\]\((.+)\)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex HubPath = new(
        @"^/hub/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}/[a-z0-9-]+$",
        RegexOptions.Compiled);
    private static readonly Regex HubMedia = new(
        @"^/v1/hub/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}/media/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
        RegexOptions.Compiled);
    private static readonly Regex WorkspaceMedia = new(
        @"^/v1/businesses/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}/content/media/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
        RegexOptions.Compiled);
    private static readonly Regex YouTubeId = new(@"^[A-Za-z0-9_-]{11}$", RegexOptions.Compiled);
    private static readonly Regex VimeoId = new(@"^\d{6,12}$", RegexOptions.Compiled);

    public static IReadOnlyList<ContentBlock> Parse(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        return Regex.Split(body.Replace("\r\n", "\n").Trim(), @"\n{2,}")
            .Select(ParseChunk)
            .Where(block => block.Kind != ContentBlockKind.Paragraph || !string.IsNullOrWhiteSpace(block.Text) || block.Items.Count > 0)
            .ToList();
    }

    public static string Serialize(IReadOnlyList<ContentBlock> blocks)
    {
        var parts = blocks.Select(SerializeBlock).Where(part => !string.IsNullOrWhiteSpace(part));
        return string.Join("\n\n", parts);
    }

    public static bool IsSafeHref(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var value = url.Trim();
        if (value.StartsWith("/hub/", StringComparison.OrdinalIgnoreCase))
        {
            return HubPath.IsMatch(value);
        }

        if (value.StartsWith("/v1/hub/", StringComparison.OrdinalIgnoreCase))
        {
            return HubMedia.IsMatch(value);
        }

        if (value.StartsWith("/v1/businesses/", StringComparison.OrdinalIgnoreCase))
        {
            return WorkspaceMedia.IsMatch(value);
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        if (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (IPAddress.TryParse(uri.Host.Trim('[', ']'), out var address))
        {
            return address.AddressFamily is System.Net.Sockets.AddressFamily.InterNetwork or System.Net.Sockets.AddressFamily.InterNetworkV6
                && !IsPrivate(address);
        }

        var host = uri.Host.TrimEnd('.');
        return !host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            && !host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase)
            && !host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryVideoEmbed(string? url, out string embedUrl, out string provider)
    {
        embedUrl = string.Empty;
        provider = string.Empty;
        if (string.IsNullOrWhiteSpace(url) || !IsSafeHref(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.TrimEnd('.').ToLowerInvariant();
        if (host is "youtu.be" or "www.youtu.be")
        {
            var id = uri.AbsolutePath.Trim('/');
            if (!YouTubeId.IsMatch(id)) return false;
            embedUrl = "https://www.youtube-nocookie.com/embed/" + id;
            provider = "YouTube";
            return true;
        }

        if (host.EndsWith("youtube.com", StringComparison.Ordinal) || host.EndsWith("youtube-nocookie.com", StringComparison.Ordinal))
        {
            var id = QueryValue(uri.Query, "v");
            if (string.IsNullOrWhiteSpace(id) && uri.AbsolutePath.Contains("/embed/", StringComparison.OrdinalIgnoreCase))
            {
                id = uri.AbsolutePath.Split("/embed/", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault()?.Split('/')[0];
            }

            if (string.IsNullOrWhiteSpace(id) || !YouTubeId.IsMatch(id)) return false;
            embedUrl = "https://www.youtube-nocookie.com/embed/" + id;
            provider = "YouTube";
            return true;
        }

        if (host.EndsWith("vimeo.com", StringComparison.Ordinal))
        {
            var id = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
            if (string.IsNullOrWhiteSpace(id) || !VimeoId.IsMatch(id)) return false;
            embedUrl = "https://player.vimeo.com/video/" + id;
            provider = "Vimeo";
            return true;
        }

        return false;
    }

    private static string? QueryValue(string query, string key)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;
        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair[0].Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Length > 1 ? Uri.UnescapeDataString(pair[1]) : "";
            }
        }

        return null;
    }

    private static ContentBlock ParseChunk(string chunk)
    {
        var lines = chunk.Split('\n').Select(line => line.TrimEnd()).Where(line => line.Length > 0).ToArray();
        if (lines.Length == 0)
        {
            return new ContentBlock(ContentBlockKind.Paragraph, "", [], null);
        }

        if (lines.Length == 1 && Video.Match(lines[0]) is { Success: true } video)
        {
            return new ContentBlock(ContentBlockKind.Video, video.Groups[1].Value, [], video.Groups[2].Value.Trim());
        }

        if (lines.Length == 1 && Image.Match(lines[0]) is { Success: true } image)
        {
            return new ContentBlock(ContentBlockKind.Image, image.Groups[1].Value, [], image.Groups[2].Value.Trim());
        }

        if (lines.All(line => line.StartsWith("> ")))
        {
            return new ContentBlock(ContentBlockKind.Quote, string.Join("\n", lines.Select(line => line[2..])), [], null);
        }

        if (lines.All(line => line.StartsWith("- ")))
        {
            return new ContentBlock(ContentBlockKind.List, "", lines.Select(line => line[2..]).ToArray(), null);
        }

        if (lines[0].StartsWith("## "))
        {
            return new ContentBlock(ContentBlockKind.Heading, lines[0][3..], [], null);
        }

        if (lines[0].StartsWith("# "))
        {
            return new ContentBlock(ContentBlockKind.Heading, lines[0][2..], [], null);
        }

        return new ContentBlock(ContentBlockKind.Paragraph, string.Join("\n", lines), [], null);
    }

    private static string SerializeBlock(ContentBlock block) => block.Kind switch
    {
        ContentBlockKind.Heading => "# " + block.Text.Trim(),
        ContentBlockKind.Quote => string.Join("\n", block.Text.Split('\n').Select(line => "> " + line.TrimEnd())),
        ContentBlockKind.List => string.Join("\n", block.Items.Select(item => "- " + item.Trim()).Where(item => item.Length > 2)),
        ContentBlockKind.Image => $"![{block.Text.Trim()}]({block.Url?.Trim()})",
        ContentBlockKind.Video => $"!video[{block.Text.Trim()}]({block.Url?.Trim()})",
        _ => block.Text.Trim()
    };

    private static bool IsPrivate(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] switch
        {
            10 => true,
            127 => true,
            169 when bytes[1] == 254 => true,
            172 when bytes[1] is >= 16 and <= 31 => true,
            192 when bytes[1] == 168 => true,
            _ => false
        };
    }
}
