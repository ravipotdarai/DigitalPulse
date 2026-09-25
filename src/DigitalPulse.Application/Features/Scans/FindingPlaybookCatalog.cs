using DigitalPulse.Domain.Scans;

namespace DigitalPulse.Application.Features.Scans;

public sealed record PlaybookStepDraft(string Title, string Detail, string? OfficialUrl);

public static class FindingPlaybookCatalog
{
    public static (string Path, string Code) Assign(string category, FindingAutomationState automation, string title)
    {
        if (title.Contains("not connected", StringComparison.OrdinalIgnoreCase)
            || title.Contains("not healthy", StringComparison.OrdinalIgnoreCase)
            || title.Contains("reauth", StringComparison.OrdinalIgnoreCase))
        {
            return ("ConnectFirst", "connect-platform");
        }

        if (category.Equals("SearchConsole", StringComparison.OrdinalIgnoreCase)
            && (title.Contains("sitemap", StringComparison.OrdinalIgnoreCase) || title.Contains("inspect", StringComparison.OrdinalIgnoreCase)))
        {
            return ("OfficialWrite", "search-console-write");
        }

        if (automation == FindingAutomationState.Blocked)
        {
            return ("ConnectFirst", "connect-platform");
        }

        return category.ToUpperInvariant() switch
        {
            "WEBSITE" or "PAGE" or "SEO" => ("AssistedPlaybook", "edit-website"),
            "VISION" => ("AssistedPlaybook", "publish-vision"),
            "CONTACT" => ("AssistedPlaybook", "edit-contact"),
            "SEARCHCONSOLE" => ("AssistedPlaybook", "search-console-read"),
            "ADS" or "GOOGLEADS" => ("AssistedPlaybook", "google-ads-ui"),
            "ANALYTICS" => ("AssistedPlaybook", "ga4-tag"),
            "PLATFORM" => ("ConnectFirst", "connect-platform"),
            _ => ("AssistedPlaybook", "edit-website")
        };
    }

    public static IReadOnlyList<PlaybookStepDraft> Steps(string? code, string? expected, string? officialUrl)
    {
        var copy = string.IsNullOrWhiteSpace(expected) ? "the value on the identity record" : expected;
        return code switch
        {
            "connect-platform" =>
            [
                new("Open Connection Center", "Connect the official platform. DigitalPulse does not invent a live snapshot.", "/app/connections"),
                new("Complete Google login", "Use the official Google consent screen. Development grants stay on Hold.", null),
                new("Re-run the test", "Analyze the website or run DigitalPulse Check after the grant is live.", null)
            ],
            "publish-vision" =>
            [
                new("Record vision on Identity", "Add an approved VISION or MISSION fact. DigitalPulse will not invent a slogan.", "/app/identity"),
                new("Open the About page editor", "WordPress: Pages → About. Shopify: Pages → About. Custom HTML: the About file.", officialUrl),
                new("Paste the approved vision", $"Copy exactly: {copy}", null),
                new("Re-analyze the website", "DigitalPulse re-fetches About and only then can mark this resolved.", null)
            ],
            "edit-contact" =>
            [
                new("Open Contact us", "WordPress: Pages → Contact. Shopify: Pages → Contact. Custom HTML: the contact file.", officialUrl),
                new("Paste identity contact details", $"Use {copy}. Restricted facts stay off the page.", null),
                new("Add a form or mailto", "A visible form, mailto, or tel link is enough. Do not invent a third-party widget.", null),
                new("Re-analyze the website", "Verify waits for the next crawl to observe the same values.", null)
            ],
            "search-console-read" =>
            [
                new("Open Search Console", "Use the official property that matches the identity website.", "https://search.google.com/search-console"),
                new("Review the stored queries", "DigitalPulse only lists queries Google returned. Empty official rows stay empty.", null),
                new("Fix the page, not the ranking", "Change the website copy or sitemap. DigitalPulse cannot write a ranking.", officialUrl)
            ],
            "search-console-write" =>
            [
                new("Approve the Search Console write", "Submit sitemap or inspect URL from Actions after a live webmasters grant.", "/app/actions"),
                new("Reconnect if the grant is readonly", "Older tokens used webmasters.readonly. Reauthorize Search Console.", "/app/connections")
            ],
            "google-ads-ui" =>
            [
                new("Open Google Ads", "Use the official customer DigitalPulse observed. Campaigns are not mutated from here.", "https://ads.google.com/"),
                new("Change the campaign in Google Ads", $"Work from {copy}. DigitalPulse does not pause, bid, or add keywords in this slice.", null),
                new("Refresh Ads metrics", "Reconnect and refresh after the official change. Spend is never invented.", "/app/connections")
            ],
            "ga4-tag" =>
            [
                new("Create or pick a GA4 property", "Use analytics.google.com. DigitalPulse does not invent sessions.", "https://analytics.google.com/"),
                new("Install the official tag", "Paste the gtag or GTM snippet on every public page. Measurement Protocol is out of scope.", officialUrl),
                new("Reconnect Google Analytics", "After the tag is live, DigitalPulse can read official Data API rows only.", "/app/connections")
            ],
            _ =>
            [
                new("Open the website editor", "WordPress: Pages. Shopify: Online Store → Pages. Custom HTML: the matching file.", officialUrl),
                new("Paste the identity value", $"Use {copy}. Do not publish restricted facts.", null),
                new("Re-analyze the website", "Resolve waits until the next crawl observes the expected copy.", null)
            ]
        };
    }
}
