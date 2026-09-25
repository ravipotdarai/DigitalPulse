using System.Text.RegularExpressions;

namespace DigitalPulse.Domain.Safety;

public sealed record ContentSafetyResult(bool Allowed, string Detail);

public static class ContentSafety
{
    public const string BanMessage = "Sexual or pornographic content is banned on DigitalPulse.";

    private static readonly Regex Banned = new(
        @"\b(porn|pornography|xxx|onlyfans|nude|nudes|naked|erotica|erotic|fetish|bdsm|hentai|nsfw|sex\s*tape|sexual|escort|hooker|prostitute|prostitutes|camgirl|stripper|incest|bestiality|child\s*porn)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static ContentSafetyResult Assess(params string?[] parts)
    {
        var text = string.Join("\n", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ContentSafetyResult(true, "No sexual or pornographic language detected.");
        }

        return Banned.IsMatch(text)
            ? new ContentSafetyResult(false, BanMessage)
            : new ContentSafetyResult(true, "No sexual or pornographic language detected.");
    }

    public static void EnsureAllowed(params string?[] parts)
    {
        var result = Assess(parts);
        if (!result.Allowed)
        {
            throw new InvalidOperationException(result.Detail);
        }
    }
}
