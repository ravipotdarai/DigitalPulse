using DigitalPulse.Contracts.Social;
using DigitalPulse.Domain.Safety;

namespace DigitalPulse.Application.Features.Social;

public static class SocialPostChecks
{
    public static SocialPostReview Evaluate(string title, string body, bool analyticsLive)
    {
        var parts = SocialDraft.Parse(body);
        var safety = ContentSafety.Assess(title, parts.Text, parts.Location);
        var notes = new List<string>();
        var heading = title.Trim();
        if (heading.Length < 8) notes.Add("Title is too short for search.");
        if (heading.Length > 70) notes.Add("Title is longer than 70 characters.");
        if (parts.Text.Length < 40) notes.Add("Description is thin for a search snippet.");
        if (string.IsNullOrWhiteSpace(parts.Location)) notes.Add("Add a location for local SEO.");
        if (string.IsNullOrWhiteSpace(parts.ImageUrl) && parts.ImageFileId is null)
        {
            notes.Add("Add an image. Posts with media are easier to find and share.");
        }

        var tokens = heading.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(word => word.Length > 3)
            .ToArray();
        if (tokens.Length > 0 && parts.Text.Length > 0 && tokens.Count(word => parts.Text.Contains(word, StringComparison.OrdinalIgnoreCase)) == 0)
        {
            notes.Add("Repeat a title word in the description so search can match the post.");
        }

        return new SocialPostReview(
            safety.Allowed ? "Pass" : "Banned",
            safety.Detail,
            notes.Count == 0 ? "Ready" : "Needs work",
            notes,
            analyticsLive ? "Supported" : "Not connected",
            analyticsLive
                ? "Google Analytics is connected. DigitalPulse will not invent views or events for this post."
                : "Google Analytics is not authorized. Measurement is not supported until you connect GA4.");
    }
}
