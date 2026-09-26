using System.Text.Json;
using System.Text.RegularExpressions;

namespace DigitalPulse.Domain.Content;

public sealed record ContentSeoCheck(string Code, string Label, bool Passed, string Note);

public sealed record ContentSeoSnapshot(
    IReadOnlyList<ContentSeoCheck> Checks,
    IReadOnlyList<ContentSeoCheck> AeoChecks,
    int EntitiesMentioned,
    int EntitiesTotal);

public sealed record ContentSeoResult(
    string SearchIntent,
    int Passed,
    int Total,
    int Score,
    string MetaTitle,
    string MetaDescription,
    IReadOnlyList<string> Notes,
    int ReadabilityScore,
    int AeoScore,
    int SlugScore,
    int InternalLinkScore,
    int EntityCoverageScore,
    IReadOnlyList<ContentSeoCheck> Checks,
    IReadOnlyList<ContentSeoCheck> AeoChecks,
    int EntitiesMentioned,
    int EntitiesTotal);

public static class ContentSeo
{
    public const int HealthTotal = 7;
    public const int AeoTotal = 6;

    public static ContentSeoResult Evaluate(
        string title,
        string excerpt,
        string body,
        string? focusKeyword,
        string? canonicalUrl,
        string? slug = null,
        string? metaTitle = null,
        string? metaDescription = null,
        IReadOnlyList<string>? entities = null)
    {
        var heading = title.Trim();
        var text = body.Trim();
        var blurb = excerpt.Trim();
        var keyword = focusKeyword?.Trim() ?? string.Empty;
        var haystack = $"{heading}\n{text}";
        var names = (entities ?? [])
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Select(static name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var storedTitle = string.IsNullOrWhiteSpace(metaTitle)
            ? (heading.Length <= 60 ? heading : heading[..60])
            : metaTitle.Trim();
        var storedDescription = string.IsNullOrWhiteSpace(metaDescription)
            ? (blurb.Length >= 40
                ? (blurb.Length <= 160 ? blurb : blurb[..160])
                : (text.Length <= 160 ? text : text[..160]))
            : metaDescription.Trim();

        var intent = ClassifyIntent(heading, keyword);
        var intentSignaled = HasIntentSignal(heading, keyword);
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var sentences = text.Split('.', StringSplitOptions.RemoveEmptyEntries).Length;
        var avg = sentences == 0 ? words.Length : words.Length / Math.Max(1, sentences);
        var readable = avg is >= 8 and <= 24;
        var headingStructure = Regex.IsMatch(text, @"^#{1,3}\s+\S", RegexOptions.Multiline);
        var internalLinks = text.Contains("](/", StringComparison.Ordinal) || text.Contains("href=\"/", StringComparison.OrdinalIgnoreCase);
        var mentioned = names.Count(name => haystack.Contains(name, StringComparison.OrdinalIgnoreCase));
        var entityScore = names.Count == 0 ? 0 : (int)Math.Round(100d * mentioned / names.Count);
        var titleReady = heading.Length is >= 12 and <= 70 && (keyword.Length < 3 || heading.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        var metaReady = storedDescription.Length is >= 40 and <= 160;

        var checks = new List<ContentSeoCheck>
        {
            Check("search-intent", "Search intent", intentSignaled,
                intentSignaled
                    ? $"{intent}. Classified from the title and focus keyword, not from a ranking model."
                    : "Add how, what, vs, buy, or a service so search intent is visible in the title."),
            Check("title", "Title optimization", titleReady,
                titleReady
                    ? "Title is 12–70 characters and includes the focus keyword when one is set."
                    : "Title should be 12–70 characters and repeat the focus keyword."),
            Check("meta-description", "Meta description", metaReady,
                metaReady
                    ? "Snippet is 40–160 characters."
                    : "Meta description should be 40–160 characters."),
            Check("headings", "Heading structure", headingStructure,
                headingStructure
                    ? "The body has markdown headings."
                    : "Add headings so the article has structure."),
            Check("internal-links", "Internal links", internalLinks,
                internalLinks
                    ? "The body links to another DigitalPulse path."
                    : "Add an internal link to another DigitalPulse path."),
            Check("entity-coverage", "Entity coverage", names.Count > 0 && mentioned > 0,
                names.Count == 0
                    ? "Entity coverage stays 0 until this business has approved facts, services, or projects, and the article names them."
                    : mentioned == 0
                        ? $"Add a stored record name. {names.Count} on record, 0 mentioned."
                        : $"{mentioned}/{names.Count} stored records appear in the title or body."),
            Check("readability", "Readability", readable,
                readable
                    ? "Average sentence length is in the readable band."
                    : "Sentences look too long or too short for a readable article.")
        };

        var aeo = AeoChecks(text);
        var aeoPassed = aeo.Count(item => item.Passed);
        var healthPassed = checks.Count(item => item.Passed);
        var notes = checks.Where(item => !item.Passed).Select(item => item.Note).ToList();
        notes.AddRange(aeo.Where(item => !item.Passed).Select(item => item.Note));
        if (storedTitle.Length is < 12 or > 70) notes.Add("Meta title should be 12–70 characters.");
        if (string.IsNullOrWhiteSpace(canonicalUrl) || !canonicalUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            notes.Add("Add an https canonical URL when this article has a public page.");
        }

        var slugValue = (slug ?? string.Empty).Trim();
        var slugScore = slugValue.Length is >= 8 and <= 60 && slugValue.Contains('-') ? 100 : slugValue.Length >= 3 ? 50 : 0;
        if (slugScore < 100) notes.Add("Slug score is the hyphenated public path, not a ranking promise.");

        return new ContentSeoResult(
            intent,
            healthPassed,
            HealthTotal,
            (int)Math.Round(100d * healthPassed / HealthTotal),
            storedTitle,
            storedDescription,
            notes,
            readable ? 100 : 0,
            (int)Math.Round(100d * aeoPassed / AeoTotal),
            slugScore,
            internalLinks ? 100 : 0,
            entityScore,
            checks,
            aeo,
            mentioned,
            names.Count);
    }

    public static string ClassifyIntent(string title, string? focusKeyword)
    {
        var hay = $"{title} {focusKeyword}";
        if (ContainsAny(hay, "buy", "price", "hire", "book", "cost")) return "Transactional";
        if (ContainsAny(hay, " vs ", "vs.", "compare", "best")) return "Commercial";
        if (ContainsAny(hay, "near me", "near")) return "Navigational";
        return "Informational";
    }

    public static string NotesJson(IReadOnlyList<string> notes) => JsonSerializer.Serialize(notes);

    public static IReadOnlyList<string> ReadNotes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        return JsonSerializer.Deserialize<List<string>>(json) ?? [];
    }

    public static string SnapshotJson(ContentSeoResult result) =>
        JsonSerializer.Serialize(new ContentSeoSnapshot(result.Checks, result.AeoChecks, result.EntitiesMentioned, result.EntitiesTotal));

    public static ContentSeoSnapshot ReadSnapshot(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() is "[]" or "{}")
        {
            return new ContentSeoSnapshot([], [], 0, 0);
        }

        return JsonSerializer.Deserialize<ContentSeoSnapshot>(json) ?? new ContentSeoSnapshot([], [], 0, 0);
    }

    private static IReadOnlyList<ContentSeoCheck> AeoChecks(string text) =>
    [
        Check("faq", "FAQ", ContainsAny(text, "FAQ", "Question:", "What should", "What is"),
            ContainsAny(text, "FAQ", "Question:", "What should", "What is")
                ? "FAQ or question copy is present."
                : "Add an FAQ or a question so answer engines can quote a stored answer."),
        Check("howto", "How-to steps", (text.Contains("1.", StringComparison.Ordinal) && text.Contains("2.", StringComparison.Ordinal)) || ContainsAny(text, "## How-to", "How to"),
            text.Contains("1.", StringComparison.Ordinal) && text.Contains("2.", StringComparison.Ordinal)
                ? "Numbered steps are present."
                : "Add numbered how-to steps."),
        Check("definition", "Definitions", ContainsAny(text, "## Definition", " is a ", "What is "),
            ContainsAny(text, "## Definition", " is a ", "What is ")
                ? "A definition block or sentence is present."
                : "Add a definition from a stored record."),
        Check("comparison", "Comparison", ContainsAny(text, " vs ", "compared to", "## Comparison"),
            ContainsAny(text, " vs ", "compared to", "## Comparison")
                ? "A comparison is present."
                : "Add a comparison only when stored records support it."),
        Check("key-facts", "Key facts", ContainsAny(text, "## Key facts", "Key considerations", "Key fact"),
            ContainsAny(text, "## Key facts", "Key considerations", "Key fact")
                ? "Key facts are listed."
                : "Add a key-facts list from approved records."),
        Check("summary", "Summary", ContainsAny(text, "## Summary", "In summary"),
            ContainsAny(text, "## Summary", "In summary")
                ? "A summary heading is present."
                : "Add a short summary section.")
    ];

    private static bool HasIntentSignal(string title, string keyword) =>
        ContainsAny($"{title} {keyword}", "how", "what", "why", "guide", "faq", " vs ", "compare", "best", "buy", "price", "hire", "book", "cost", "near");

    private static bool ContainsAny(string text, params string[] markers) =>
        markers.Any(marker => text.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static ContentSeoCheck Check(string code, string label, bool passed, string note) =>
        new(code, label, passed, note);
}
