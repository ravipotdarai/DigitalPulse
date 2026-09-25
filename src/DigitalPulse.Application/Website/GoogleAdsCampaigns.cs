using System.Text.Json;

namespace DigitalPulse.Application.Website;

public sealed record GoogleAdsCampaignRow(string Id, string Name, string Status, double? Impressions, double? Clicks);

public static class GoogleAdsCampaigns
{
    public static IReadOnlyList<string> CustomerIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("resourceNames", out var names) || names.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var ids = new List<string>();
            foreach (var name in names.EnumerateArray())
            {
                var raw = name.GetString();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                var id = raw.StartsWith("customers/", StringComparison.OrdinalIgnoreCase)
                    ? raw["customers/".Length..]
                    : raw;
                if (id.Length > 0 && id.All(char.IsDigit))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static IReadOnlyList<GoogleAdsCampaignRow> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var items = new List<GoogleAdsCampaignRow>();
            foreach (var row in results.EnumerateArray())
            {
                if (!row.TryGetProperty("campaign", out var campaign))
                {
                    continue;
                }

                var id = ReadString(campaign, "id") ?? ReadString(campaign, "resourceName");
                var name = ReadString(campaign, "name");
                if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var metrics = row.TryGetProperty("metrics", out var m) ? m : default;
                items.Add(new(
                    id ?? name!,
                    name ?? id!,
                    ReadString(campaign, "status") ?? "UNSPECIFIED",
                    ReadNumber(metrics, "impressions"),
                    ReadNumber(metrics, "clicks")));
            }

            return items;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
            ? value.GetString()
            : null;

    private static double? ReadNumber(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDouble(out var n) => n,
            JsonValueKind.String when double.TryParse(value.GetString(), out var n) => n,
            _ => null
        };
    }
}
