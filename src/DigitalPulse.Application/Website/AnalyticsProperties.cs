using System.Text.Json;

namespace DigitalPulse.Application.Website;

public sealed record AnalyticsPropertyOption(string Property, string Label);

public static class AnalyticsProperties
{
    public static IReadOnlyList<AnalyticsPropertyOption> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("accountSummaries", out var accounts) ||
                accounts.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var items = new List<AnalyticsPropertyOption>();
            foreach (var account in accounts.EnumerateArray())
            {
                var accountName = account.TryGetProperty("displayName", out var an) ? an.GetString() : null;
                if (!account.TryGetProperty("propertySummaries", out var properties) ||
                    properties.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var property in properties.EnumerateArray())
                {
                    var id = property.TryGetProperty("property", out var p) ? p.GetString() : null;
                    if (string.IsNullOrWhiteSpace(id) ||
                        !id.StartsWith("properties/", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var name = property.TryGetProperty("displayName", out var dn) ? dn.GetString() : id;
                    var label = string.IsNullOrWhiteSpace(accountName)
                        ? (name ?? id)
                        : $"{accountName} · {name}";
                    items.Add(new(id, label));
                }
            }

            return items;
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
