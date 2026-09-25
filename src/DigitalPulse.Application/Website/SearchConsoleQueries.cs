using System.Text.Json;

namespace DigitalPulse.Application.Website;

public sealed record SearchConsoleQueryRow(string Query, double Clicks, double Impressions, double Ctr, double Position);

public static class SearchConsoleQueries
{
    public static IReadOnlyList<SearchConsoleQueryRow> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var items = new List<SearchConsoleQueryRow>();
            foreach (var row in rows.EnumerateArray())
            {
                if (!row.TryGetProperty("keys", out var keys) || keys.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                var query = keys.EnumerateArray().FirstOrDefault().GetString();
                if (string.IsNullOrWhiteSpace(query))
                {
                    continue;
                }

                items.Add(new(
                    query,
                    ReadNumber(row, "clicks"),
                    ReadNumber(row, "impressions"),
                    ReadNumber(row, "ctr"),
                    ReadNumber(row, "position")));
            }

            return items;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static double ReadNumber(JsonElement row, string name) =>
        row.TryGetProperty(name, out var value) && value.TryGetDouble(out var number) ? number : 0;
}
