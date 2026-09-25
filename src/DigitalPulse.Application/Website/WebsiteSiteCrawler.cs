using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;

namespace DigitalPulse.Application.Website;

public sealed record CrawledPage(
    string Url,
    WebsiteFetchResult Fetch,
    HtmlSignals? Signals);

public static class WebsiteSiteCrawler
{
    public const int PageCap = 25;

    public static async Task<IReadOnlyList<CrawledPage>> CrawlAsync(
        IWebsiteFetcher fetcher,
        string website,
        CancellationToken cancellationToken)
    {
        if (!SafeUrlPolicy.TryValidate(website, out var origin, out _))
        {
            var blocked = await fetcher.FetchAsync(website, cancellationToken);
            return [new CrawledPage(website, blocked, null)];
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();
        Enqueue(queue, seen, origin, origin.AbsoluteUri);

        var sitemap = await fetcher.FetchAsync(new Uri(origin, "/sitemap.xml").AbsoluteUri, cancellationToken);
        if (sitemap.Reached)
        {
            foreach (var loc in HtmlSignalParser.ExtractSitemapLocs(sitemap.Html))
            {
                Enqueue(queue, seen, origin, loc);
            }
        }

        var pages = new List<CrawledPage>();
        var fetched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (queue.Count > 0 && pages.Count < PageCap)
        {
            var next = queue.Dequeue();
            if (!fetched.Add(next))
            {
                continue;
            }

            var fetch = await fetcher.FetchAsync(next, cancellationToken);
            var signals = fetch.Reached ? HtmlSignalParser.Parse(fetch.Html) : null;
            pages.Add(new CrawledPage(fetch.FinalUrl ?? next, fetch, signals));
            if (!fetch.Reached || signals is null)
            {
                continue;
            }

            foreach (var href in HtmlSignalParser.ExtractHrefs(fetch.Html))
            {
                Enqueue(queue, seen, origin, href);
            }
        }

        return pages.Count == 0
            ? [new CrawledPage(origin.AbsoluteUri, await fetcher.FetchAsync(origin.AbsoluteUri, cancellationToken), null)]
            : pages;
    }

    private static void Enqueue(Queue<string> queue, HashSet<string> seen, Uri origin, string raw)
    {
        if (!TryNormalize(origin, raw, out var url) || !seen.Add(url))
        {
            return;
        }

        queue.Enqueue(url);
    }

    public static bool TryNormalize(Uri origin, string raw, out string url)
    {
        url = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var trimmed = raw.Trim();
        if (trimmed.StartsWith('#') || trimmed.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!Uri.TryCreate(origin, trimmed, out var resolved) || !SafeUrlPolicy.TryValidate(resolved.AbsoluteUri, out var safe, out _))
        {
            return false;
        }

        if (!string.Equals(safe.Host, origin.Host, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var ext = Path.GetExtension(safe.AbsolutePath);
        if (ext is ".pdf" or ".jpg" or ".jpeg" or ".png" or ".gif" or ".svg" or ".webp" or ".zip" or ".css" or ".js")
        {
            return false;
        }

        url = safe.GetLeftPart(UriPartial.Path).TrimEnd('/') + "/";
        return true;
    }
}
