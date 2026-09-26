using System.Text.Json;
using DigitalPulse.Contracts.Connections;

namespace DigitalPulse.Application.Features.Connections;

public sealed record OfficialAccountChoice(string Id, string Label, string Kind, string? AccessToken);

public static class OfficialAccounts
{
    public static IReadOnlyList<ConnectionAccountOption> Public(IEnumerable<OfficialAccountChoice> items) =>
        items.Select(item => new ConnectionAccountOption(item.Id, item.Label, item.Kind)).ToList();

    public static IReadOnlyList<OfficialAccountChoice> FacebookPages(string? json)
    {
        var items = new List<OfficialAccountChoice>();
        foreach (var row in Array(json, "data"))
        {
            var id = Text(row, "id");
            if (string.IsNullOrWhiteSpace(id)) continue;
            items.Add(new(id, Text(row, "name") ?? id, "Page", Text(row, "access_token")));
        }

        return items;
    }

    public static IReadOnlyList<OfficialAccountChoice> InstagramAccounts(string? json)
    {
        var items = new List<OfficialAccountChoice>();
        foreach (var row in Array(json, "data"))
        {
            if (!row.TryGetProperty("instagram_business_account", out var ig) || ig.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var id = Text(ig, "id");
            if (string.IsNullOrWhiteSpace(id)) continue;
            var username = Text(ig, "username") ?? id;
            var page = Text(row, "name");
            items.Add(new(id, string.IsNullOrWhiteSpace(page) ? username : $"{page} · @{username}", "Instagram", Text(row, "access_token")));
        }

        return items;
    }

    public static IReadOnlyList<OfficialAccountChoice> YouTubeChannels(string? json)
    {
        var items = new List<OfficialAccountChoice>();
        foreach (var row in Array(json, "items"))
        {
            var id = Text(row, "id");
            if (string.IsNullOrWhiteSpace(id)) continue;
            var title = row.TryGetProperty("snippet", out var snippet) && snippet.ValueKind == JsonValueKind.Object
                ? Text(snippet, "title")
                : id;
            items.Add(new(id, title ?? id, "Channel", null));
        }

        return items;
    }

    public static IReadOnlyList<OfficialAccountChoice> SearchConsoleSites(string? json)
    {
        var items = new List<OfficialAccountChoice>();
        foreach (var row in Array(json, "siteEntry"))
        {
            var url = Text(row, "siteUrl");
            if (string.IsNullOrWhiteSpace(url)) continue;
            items.Add(new(url, url, "Site", null));
        }

        return items;
    }

    public static IReadOnlyList<OfficialAccountChoice> GoogleAccounts(string? json)
    {
        var items = new List<OfficialAccountChoice>();
        foreach (var row in Array(json, "accounts"))
        {
            var name = Text(row, "name");
            if (string.IsNullOrWhiteSpace(name)) continue;
            var label = Text(row, "accountName") ?? name;
            items.Add(new(name, label, "GoogleAccount", null));
        }

        return items;
    }

    public static IReadOnlyList<OfficialAccountChoice> GoogleLocations(string? json)
    {
        var items = new List<OfficialAccountChoice>();
        foreach (var row in Array(json, "locations"))
        {
            var name = Text(row, "name");
            if (string.IsNullOrWhiteSpace(name)) continue;
            var title = row.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : name;
            items.Add(new(name, title ?? name, "Location", null));
        }

        return items;
    }

    public static IReadOnlyList<OfficialAccountChoice> GoogleAdsCustomers(string? json)
    {
        var items = new List<OfficialAccountChoice>();
        foreach (var row in Array(json, "resourceNames"))
        {
            var name = row.ValueKind == JsonValueKind.String ? row.GetString() : null;
            if (string.IsNullOrWhiteSpace(name)) continue;
            items.Add(new(name, name, "AdsCustomer", null));
        }

        return items;
    }

    public static IReadOnlyList<OfficialAccountChoice> LinkedInPerson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var id = Text(root, "sub") ?? Text(root, "id");
            if (string.IsNullOrWhiteSpace(id)) return [];
            var name = Text(root, "name") ?? id;
            return [new(id, name, "Member", null)];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<JsonElement> Array(string? json, string property)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(property, out var array) || array.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return array.EnumerateArray().Select(row => row.Clone()).ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? Text(JsonElement row, string name) =>
        row.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
