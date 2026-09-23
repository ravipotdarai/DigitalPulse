using DigitalPulse.Application.Abstractions;
using DigitalPulse.Domain.Platforms;
using DigitalPulse.Domain.Website;

namespace DigitalPulse.Application.Website;

public sealed record DraftSearchObservation(
    SearchObservationCategory Category,
    SearchObservationSeverity Severity,
    string Title,
    string Detail,
    string? ExpectedValue,
    string? ObservedValue,
    string Recommendation);

public static class WebsiteObservations
{
    public static IReadOnlyList<DraftSearchObservation> FromSnapshot(
        string businessName,
        WebsiteSnapshot snapshot,
        HtmlSignals? signals,
        IReadOnlyCollection<PlatformConnection> connections)
    {
        var items = new List<DraftSearchObservation>();
        if (snapshot.Status == WebsiteFetchStatus.Blocked)
        {
            items.Add(new(
                SearchObservationCategory.Visibility,
                SearchObservationSeverity.High,
                "Website URL is not allowed",
                "The official website failed the crawl safety policy.",
                "A public http(s) website",
                snapshot.Error,
                "Use a public website URL. DigitalPulse will not fetch private hosts."));
            return WithSearchConsole(items, connections);
        }

        if (snapshot.Status == WebsiteFetchStatus.Unreachable)
        {
            items.Add(new(
                SearchObservationCategory.Visibility,
                SearchObservationSeverity.High,
                "Website is unreachable",
                "The homepage did not return usable HTML. This is a fetch result, not a search ranking.",
                "HTTP response with HTML",
                snapshot.Error,
                "Confirm DNS and hosting, then run website analysis again."));
            return WithSearchConsole(items, connections);
        }

        if (snapshot.Status == WebsiteFetchStatus.Skipped)
        {
            items.Add(new(
                SearchObservationCategory.Visibility,
                SearchObservationSeverity.Medium,
                "Website analysis was skipped",
                snapshot.Error ?? "The host did not fetch the public website.",
                "A fetched homepage snapshot",
                "Skipped",
                "Run analysis outside the test host, or set a public website on the identity record."));
            return WithSearchConsole(items, connections);
        }

        if (string.IsNullOrWhiteSpace(snapshot.Title))
        {
            items.Add(Seo("Title tag is missing", "Search engines use the title as the primary homepage label.", "A unique title that includes the business name", "Missing", "Add a title tag on the homepage."));
        }
        else if (!HtmlSignalParser.ContainsName(snapshot.Title, businessName))
        {
            items.Add(Seo("Title does not include the business name", "The fetched title does not contain the canonical identity name.", businessName, snapshot.Title, "Put the trading name in the homepage title."));
        }

        if (string.IsNullOrWhiteSpace(snapshot.MetaDescription))
        {
            items.Add(Seo("Meta description is missing", "The homepage has no description meta tag.", "A 50–160 character description", "Missing", "Add a meta description that matches an approved claim."));
        }
        else if (snapshot.MetaDescription.Length is < 50 or > 160)
        {
            items.Add(Seo(
                "Meta description length is off",
                "The fetched description is shorter than 50 or longer than 160 characters.",
                "50–160 characters",
                $"{snapshot.MetaDescription.Length} characters",
                "Rewrite the meta description to a typical SERP length."));
        }

        if (string.IsNullOrWhiteSpace(snapshot.H1))
        {
            items.Add(Seo("H1 is missing", "The homepage has no H1 heading.", "One H1 that names the business or offer", "Missing", "Add a single H1 on the homepage."));
        }

        if (string.IsNullOrWhiteSpace(snapshot.CanonicalUrl))
        {
            items.Add(Seo("Canonical URL is missing", "No rel=canonical was found on the homepage.", "A canonical link to the official URL", "Missing", "Add a canonical link to the preferred homepage URL."));
        }

        if (!string.IsNullOrWhiteSpace(snapshot.Robots) && snapshot.Robots.Contains("noindex", StringComparison.OrdinalIgnoreCase))
        {
            items.Add(new(
                SearchObservationCategory.Visibility,
                SearchObservationSeverity.High,
                "Homepage is marked noindex",
                "The fetched robots meta instructs crawlers not to index this page.",
                "index,follow unless the page must stay private",
                snapshot.Robots,
                "Remove noindex if this page should appear in search."));
        }

        if (snapshot.WordCount < 200)
        {
            items.Add(Seo("Homepage copy is thin", "The fetched HTML has fewer than 200 words of visible text.", "At least 200 words of unique copy", $"{snapshot.WordCount} words", "Publish a clearer about/offer section on the homepage."));
        }

        if (!snapshot.HasOgTitle)
        {
            items.Add(new(
                SearchObservationCategory.Seo,
                SearchObservationSeverity.Low,
                "Open Graph title is missing",
                "Social and some answer surfaces use og:title when sharing the page.",
                "og:title on the homepage",
                "Missing",
                "Add an og:title that matches the identity name."));
        }

        if (!snapshot.HasJsonLd)
        {
            items.Add(Aeo("No JSON-LD structured data", "Answer engines and rich results need machine-readable entities.", "JSON-LD for Organization or LocalBusiness", "Missing", "Add Organization or LocalBusiness JSON-LD."));
        }
        else if (!snapshot.HasOrganizationSchema)
        {
            items.Add(Aeo("Organization schema is missing", "JSON-LD is present but not an Organization or LocalBusiness type.", "Organization or LocalBusiness", "Other JSON-LD", "Declare the business as Organization or LocalBusiness."));
        }

        if (!snapshot.HasFaqSchema)
        {
            items.Add(Aeo("FAQ schema is missing", "AEO-related analysis looks for FAQPage markup, not invented featured snippets.", "FAQPage JSON-LD when the page answers questions", "Missing", "Add FAQPage markup only for questions the page actually answers."));
        }

        if (signals is { QuestionHeadings.Count: 0 } && !snapshot.HasFaqSchema)
        {
            items.Add(Aeo("No question-form headings", "The homepage has no H1–H3 that end with a question mark.", "At least one question the business can answer", "None", "Add a short FAQ section if customers ask repeat questions."));
        }

        if (!snapshot.ContainsBusinessName)
        {
            items.Add(new(
                SearchObservationCategory.Visibility,
                SearchObservationSeverity.Medium,
                "Business name not found in page text",
                "The fetched HTML does not contain the canonical business name. This is not a ranking.",
                businessName,
                snapshot.Url,
                "Publish the same trading name on the homepage."));
        }

        return WithSearchConsole(items, connections);
    }

    private static List<DraftSearchObservation> WithSearchConsole(
        List<DraftSearchObservation> items,
        IReadOnlyCollection<PlatformConnection> connections)
    {
        var gsc = connections.FirstOrDefault(c => c.PlatformCode.Equals("SEARCH_CONSOLE", StringComparison.OrdinalIgnoreCase));
        if (gsc is null || gsc.Status != ConnectionStatus.Connected)
        {
            items.Add(new(
                SearchObservationCategory.SearchConsole,
                SearchObservationSeverity.Medium,
                "Search Console is not connected",
                "Impressions, queries, and index coverage require an authorized Search Console grant. DigitalPulse will not invent them.",
                "A connected Search Console property",
                gsc?.Status.ToString() ?? "Not connected",
                "Connect Search Console in Connection Center when a live Google grant is available."));
            return items;
        }

        if (string.Equals(gsc.GrantKind, "Development", StringComparison.OrdinalIgnoreCase))
        {
            items.Add(new(
                SearchObservationCategory.SearchConsole,
                SearchObservationSeverity.Medium,
                "Search Console snapshot is unavailable",
                "This connection uses a development grant. Clicks, impressions, and top queries are not invented.",
                "A live Search Console grant",
                gsc.GrantKind,
                "Keep the adapter contract. Configure official Search Console later."));
            return items;
        }

        items.Add(new(
            SearchObservationCategory.SearchConsole,
            SearchObservationSeverity.Low,
            "Search Console has no live reader yet",
            "The connection is authorized, but this phase does not call the Search Console API.",
            "An evidence-backed coverage snapshot",
            "No snapshot stored",
            "Do not treat this as index coverage until a live reader exists."));
        return items;
    }

    private static DraftSearchObservation Seo(string title, string detail, string expected, string observed, string recommendation) =>
        new(SearchObservationCategory.Seo, SearchObservationSeverity.Medium, title, detail, expected, observed, recommendation);

    private static DraftSearchObservation Aeo(string title, string detail, string expected, string observed, string recommendation) =>
        new(SearchObservationCategory.Aeo, SearchObservationSeverity.Low, title, detail, expected, observed, recommendation);
}
