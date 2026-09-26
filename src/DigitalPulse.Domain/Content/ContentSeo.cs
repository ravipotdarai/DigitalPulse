using System.Text.Json;

namespace DigitalPulse.Domain.Content;

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
    int EntityCoverageScore);

public static class ContentSeo
{
    public static ContentSeoResult Evaluate(string title, string excerpt, string body, string? focusKeyword, string? canonicalUrl, string? slug = null)
    {
        var notes = new List<string>();
        var heading = title.Trim();
        var text = body.Trim();
        var blurb = excerpt.Trim();
        var keyword = focusKeyword?.Trim() ?? string.Empty;
        var passed = 0;
        const int total = 8;

        if (heading.Length is >= 12 and <= 70) passed++;
        else notes.Add("Title should be 12–70 characters for a search snippet.");

        if (blurb.Length is >= 40 and <= 160) passed++;
        else notes.Add("Excerpt should be 40–160 characters so search can show a snippet.");

        if (text.Length >= 400) passed++;
        else notes.Add("Body is thinner than 400 characters. Search has little to index.");

        if (text.Contains('#') || text.Contains('\n')) passed++;
        else notes.Add("Add headings or short paragraphs so the article has structure.");

        if (keyword.Length >= 3 && heading.Contains(keyword, StringComparison.OrdinalIgnoreCase)) passed++;
        else notes.Add("Repeat the focus keyword in the title, or set a focus keyword.");

        if (keyword.Length >= 3 && text.Contains(keyword, StringComparison.OrdinalIgnoreCase)) passed++;
        else notes.Add("Use the focus keyword once in the body.");

        if (!string.IsNullOrWhiteSpace(canonicalUrl) && canonicalUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) passed++;
        else notes.Add("Add an https canonical URL when this article has a public page.");

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var sentences = text.Split('.', StringSplitOptions.RemoveEmptyEntries).Length;
        var avg = sentences == 0 ? words.Length : words.Length / Math.Max(1, sentences);
        if (avg is >= 8 and <= 24) passed++;
        else notes.Add("Sentences look too long or too short for a readable article.");

        var intent = keyword.Contains("how", StringComparison.OrdinalIgnoreCase) || heading.StartsWith("how", StringComparison.OrdinalIgnoreCase)
            ? "Informational"
            : heading.Contains("vs", StringComparison.OrdinalIgnoreCase)
                ? "Commercial"
                : "Informational";

        var metaTitle = heading.Length <= 60 ? heading : heading[..60];
        var metaDescription = blurb.Length >= 40
            ? (blurb.Length <= 160 ? blurb : blurb[..160])
            : (text.Length <= 160 ? text : text[..160]);

        var readability = avg is >= 8 and <= 24 ? 100 : 0;
        var aeo = HasAnswerStructure(text) ? 100 : 0;
        if (aeo == 0) notes.Add("AEO score is 0 until the body has a question plus steps or an FAQ block.");
        var slugValue = (slug ?? string.Empty).Trim();
        var slugScore = slugValue.Length is >= 8 and <= 60 && slugValue.Contains('-') ? 100 : slugValue.Length >= 3 ? 50 : 0;
        if (slugScore < 100) notes.Add("Slug score is the hyphenated public path, not a ranking promise.");
        var internalLinks = text.Contains("](/", StringComparison.Ordinal) || text.Contains("href=\"/", StringComparison.OrdinalIgnoreCase);
        var internalLinkScore = internalLinks ? 100 : 0;
        if (internalLinkScore == 0) notes.Add("Internal-link score is 0 until the body links to another DigitalPulse path.");
        notes.Add("Entity coverage stays 0 until Graphify entities are retrieved for this article.");

        return new ContentSeoResult(
            intent,
            passed,
            total,
            total == 0 ? 0 : (int)Math.Round(100d * passed / total),
            metaTitle,
            metaDescription,
            notes,
            readability,
            aeo,
            slugScore,
            internalLinkScore,
            0);
    }

    private static bool HasAnswerStructure(string text)
    {
        var hasQuestion = text.Contains('?');
        var hasSteps = text.Contains("1.", StringComparison.Ordinal) && text.Contains("2.", StringComparison.Ordinal);
        var hasFaq = text.Contains("FAQ", StringComparison.OrdinalIgnoreCase) || text.Contains("Question:", StringComparison.OrdinalIgnoreCase);
        return hasQuestion && (hasSteps || hasFaq);
    }

    public static string NotesJson(IReadOnlyList<string> notes) => JsonSerializer.Serialize(notes);

    public static IReadOnlyList<string> ReadNotes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        return JsonSerializer.Deserialize<List<string>>(json) ?? [];
    }
}
