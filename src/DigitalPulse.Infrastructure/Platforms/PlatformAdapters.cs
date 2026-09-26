using System.Text.Json;
using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Social;
using DigitalPulse.Domain.Platforms;
using Microsoft.Extensions.Configuration;

namespace DigitalPulse.Infrastructure.Platforms;

public abstract class PlatformAdapter : IPlatformAdapter
{
    protected PlatformAdapter(
        string code,
        string name,
        string category,
        PlatformAuthMode authMode,
        string summary,
        PlatformCapabilities capabilities,
        IOfficialPlatformGateway? gateway = null,
        ILiveTokenRefresher? tokens = null)
    {
        Descriptor = new PlatformDescriptor(code, name, category, authMode, summary, capabilities);
        Gateway = gateway;
        Tokens = tokens;
    }

    public PlatformDescriptor Descriptor { get; }
    protected IOfficialPlatformGateway? Gateway { get; }
    protected ILiveTokenRefresher? Tokens { get; }
    public PlatformDescriptor Describe() => Descriptor;

    public async Task<PlatformHealthResult> HealthCheckAsync(PlatformConnection connection, CancellationToken cancellationToken)
    {
        await EnsureFreshAsync(connection, cancellationToken);
        if (connection.HasLiveCredential && Gateway is not null)
        {
            return await CallAsync(connection, LiveHealth(connection), "Healthy", "NeedsReauth", cancellationToken);
        }

        if (connection.GrantKind == "Development")
        {
            return new PlatformHealthResult("NeedsReauth", "Sign in on the official platform to connect. A development grant is not a live login.");
        }

        if (connection.Status == ConnectionStatus.Connected && connection.GrantKind == "Assisted")
        {
            return new PlatformHealthResult("Healthy", "Assisted workspace is enabled. Official writes stay operator-confirmed.");
        }

        if (connection.Status == ConnectionStatus.NeedsReauth)
        {
            return new PlatformHealthResult("NeedsReauth", connection.LastError ?? "Reauthorization is required.");
        }

        return new PlatformHealthResult("Error", connection.LastError ?? "The connection is not healthy.");
    }

    public async Task<IReadOnlyList<PlatformDiagnostic>> DiagnoseAsync(PlatformConnection connection, CancellationToken cancellationToken)
    {
        var live = connection.HasLiveCredential
            ? await HealthCheckAsync(connection, cancellationToken)
            : null;
        IReadOnlyList<PlatformDiagnostic> checks =
        [
            new("Adapter", "Pass", $"{Descriptor.Name} adapter is registered."),
            new("Authorization mode", "Pass", Descriptor.AuthMode.ToString()),
            new("Grant", connection.GrantReference is null ? "Fail" : "Pass",
                connection.GrantKind is null ? "No grant stored." : $"{connection.GrantKind} grant. Access tokens are not stored in logs."),
            new("Live provider API",
                connection.HasLiveCredential ? (live?.Status == "Healthy" ? "Pass" : "Hold") : "Hold",
                connection.HasLiveCredential
                    ? live?.Detail ?? "Official API was called with the stored grant."
                    : "Official OAuth/API is not configured on this host. DigitalPulse will not invent a live integration."),
            new("Capabilities", "Pass", Summarize(Descriptor.Capabilities))
        ];
        return checks;
    }

    public Task<PlatformPublishResult> PublishAsync(PlatformConnection connection, string title, string body, CancellationToken cancellationToken) =>
        PublishAsync(connection, title, body, null, cancellationToken);

    public virtual async Task<PlatformPublishResult> PublishAsync(
        PlatformConnection connection,
        string title,
        string body,
        PlatformPublishMedia? media,
        CancellationToken cancellationToken)
    {
        await EnsureFreshAsync(connection, cancellationToken);
        if (Descriptor.Capabilities.AssistedOnly)
        {
            return new PlatformPublishResult("Assisted", "This adapter is assisted-only. Publish the copy on the official platform yourself. Unofficial write APIs are out of scope.");
        }

        if (!Descriptor.Capabilities.CanPublish)
        {
            return new PlatformPublishResult("Assisted", $"{Descriptor.Name} does not expose an official text publish in this catalog.");
        }

        if (connection.Status != ConnectionStatus.Connected)
        {
            return new PlatformPublishResult("Blocked", "Connect the platform before attempting a publish.");
        }

        media ??= SocialDraft.ToMedia(body);
        if (connection.HasLiveCredential && Gateway is not null)
        {
            var sent = await SendOfficialPublishAsync(connection, title, body, media, cancellationToken);
            if (sent is not null)
            {
                return sent;
            }

            var request = LivePublish(connection, title, body, media);
            if (request is null)
            {
                return new PlatformPublishResult("Hold", $"{Descriptor.Name} is connected, but this write shape is not supported on the official API.");
            }

            var result = await Gateway.SendAsync(
                request.Value.Method,
                request.Value.Url,
                UseBearerToken ? connection.AccessToken : null,
                request.Value.Body,
                request.Value.Headers,
                cancellationToken);
            return Interpret(result, Descriptor.Name);
        }

        return new PlatformPublishResult(
            "Hold",
            "Live provider publish waits for an official OAuth grant. DigitalPulse will not invent a posted update.");
    }

    protected virtual Task<PlatformPublishResult?> SendOfficialPublishAsync(
        PlatformConnection connection,
        string title,
        string body,
        PlatformPublishMedia media,
        CancellationToken cancellationToken) =>
        Task.FromResult<PlatformPublishResult?>(null);

    protected static PlatformPublishResult Interpret(OfficialHttpResult result, string name)
    {
        if (result.StatusCode is 401 or 403)
        {
            return new PlatformPublishResult("Hold", "The official publish was rejected. Reauthorize the connection. Nothing was invented.");
        }

        return result.Ok
            ? new PlatformPublishResult("Published", $"Official {name} write accepted ({result.StatusCode}).")
            : new PlatformPublishResult("Hold", $"Official {name} write returned {result.StatusCode}. DigitalPulse did not invent a posted update.");
    }

    public virtual async Task<PlatformMetricsResult> MetricsAsync(PlatformConnection connection, CancellationToken cancellationToken)
    {
        await EnsureFreshAsync(connection, cancellationToken);
        if (!Descriptor.Capabilities.CanGetMetrics)
        {
            return new PlatformMetricsResult("Unavailable", $"{Descriptor.Name} does not expose metrics in this catalog.");
        }

        if (connection.Status != ConnectionStatus.Connected)
        {
            return new PlatformMetricsResult("Unavailable", "Connect the platform before requesting metrics.");
        }

        if (connection.HasLiveCredential && Gateway is not null)
        {
            var request = LiveMetrics(connection);
            if (request is null)
            {
                return new PlatformMetricsResult("Hold", $"{Descriptor.Name} is connected. This metrics query is not available on the official API for the stored scopes.");
            }

            var result = await Gateway.SendAsync(
                request.Value.Method,
                request.Value.Url,
                UseBearerToken ? connection.AccessToken : null,
                request.Value.Body,
                request.Value.Headers,
                cancellationToken);
            return result.Ok
                ? new PlatformMetricsResult("Observed", Trim(result.Body))
                : new PlatformMetricsResult("Hold", $"Official metrics returned {result.StatusCode}. Counts were not invented.");
        }

        return new PlatformMetricsResult(
            "Hold",
            "Live provider metrics wait for an official OAuth grant. DigitalPulse will not invent likes, views, or reach.");
    }

    protected virtual (HttpMethod Method, string Url, string? Body, IReadOnlyDictionary<string, string>? Headers)? LiveHealth(PlatformConnection connection) => null;

    protected virtual (HttpMethod Method, string Url, string? Body, IReadOnlyDictionary<string, string>? Headers)? LivePublish(
        PlatformConnection connection, string title, string body, PlatformPublishMedia? media) =>
        LivePublish(connection, title, body);

    protected virtual (HttpMethod Method, string Url, string? Body, IReadOnlyDictionary<string, string>? Headers)? LivePublish(
        PlatformConnection connection, string title, string body) => null;

    protected virtual (HttpMethod Method, string Url, string? Body, IReadOnlyDictionary<string, string>? Headers)? LiveMetrics(PlatformConnection connection) => null;

    protected virtual bool UseBearerToken => true;

    protected async Task EnsureFreshAsync(PlatformConnection connection, CancellationToken cancellationToken)
    {
        if (Tokens is not null)
        {
            await Tokens.EnsureFreshAsync(connection, cancellationToken);
        }
    }

    private async Task<PlatformHealthResult> CallAsync(
        PlatformConnection connection,
        (HttpMethod Method, string Url, string? Body, IReadOnlyDictionary<string, string>? Headers)? request,
        string okStatus,
        string reauthStatus,
        CancellationToken cancellationToken)
    {
        if (request is null || Gateway is null)
        {
            return new PlatformHealthResult("Healthy", $"Official {Descriptor.Name} grant is stored. No health URL is defined for this adapter.");
        }

        var result = await Gateway.SendAsync(
            request.Value.Method,
            request.Value.Url,
            UseBearerToken ? connection.AccessToken : null,
            request.Value.Body,
            request.Value.Headers,
            cancellationToken);
        if (result.StatusCode is 401 or 403)
        {
            return new PlatformHealthResult(reauthStatus, "Official API rejected the stored grant. Reauthorize.");
        }

        return result.Ok
            ? new PlatformHealthResult(okStatus, $"Official {Descriptor.Name} API responded {result.StatusCode}.")
            : new PlatformHealthResult("Error", $"Official {Descriptor.Name} API returned {result.StatusCode}.");
    }

    private static string Summarize(PlatformCapabilities caps) =>
        string.Join(", ", new[]
        {
            caps.CanRead ? "read" : null,
            caps.CanCreate ? "create" : null,
            caps.CanUpdate ? "update" : null,
            caps.CanDelete ? "delete" : null,
            caps.CanPublish ? "publish" : null,
            caps.CanGetMetrics ? "metrics" : null,
            caps.AssistedOnly ? "assisted" : null
        }.Where(x => x is not null));

    private static string Trim(string body) =>
        body.Length <= 400 ? body : body[..400] + "…";
}

public sealed class GoogleAdapter(IOfficialPlatformGateway gateway, ILiveTokenRefresher? tokens = null) : PlatformAdapter("GOOGLE", "Google", "Search", PlatformAuthMode.OAuth,
    "Business Profile and Search presence through official Google authorization.",
    new(true, true, true, false, true, true, false), gateway, tokens)
{
    protected override (HttpMethod, string, string?, IReadOnlyDictionary<string, string>?)? LiveHealth(PlatformConnection connection) =>
        (HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo", null, null);

    protected override (HttpMethod, string, string?, IReadOnlyDictionary<string, string>?)? LiveMetrics(PlatformConnection connection) =>
        (HttpMethod.Get, "https://mybusinessaccountmanagement.googleapis.com/v1/accounts", null, null);

    protected override (HttpMethod, string, string?, IReadOnlyDictionary<string, string>?)? LivePublish(
        PlatformConnection connection, string title, string body, PlatformPublishMedia? media)
    {
        var location = connection.ExternalAccount;
        if (string.IsNullOrWhiteSpace(location) ||
            location.Contains("GOOGLE", StringComparison.OrdinalIgnoreCase) ||
            !location.Contains("locations/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var draft = SocialDraft.Parse(body);
        var summary = string.IsNullOrWhiteSpace(draft.Text) ? title : $"{title}\n\n{draft.Text}";
        var image = media?.ImageUrl ?? draft.ImageUrl;
        var payload = new Dictionary<string, object?>
        {
            ["languageCode"] = "en",
            ["summary"] = summary.Length > 1500 ? summary[..1500] : summary,
            ["topicType"] = "STANDARD"
        };
        if (!string.IsNullOrWhiteSpace(image))
        {
            payload["media"] = new object[]
            {
                new Dictionary<string, string> { ["mediaFormat"] = "PHOTO", ["sourceUrl"] = image }
            };
        }

        return (HttpMethod.Post, $"https://mybusiness.googleapis.com/v4/{location.Trim('/')}/localPosts", JsonSerializer.Serialize(payload), null);
    }
}

public sealed class FacebookAdapter(IOfficialPlatformGateway gateway, ILiveTokenRefresher? tokens = null) : PlatformAdapter("FACEBOOK", "Facebook", "Social", PlatformAuthMode.OAuth,
    "Page presence through official Meta authorization.",
    new(true, true, true, false, true, true, false), gateway, tokens)
{
    protected override (HttpMethod, string, string?, IReadOnlyDictionary<string, string>?)? LiveHealth(PlatformConnection connection) =>
        (HttpMethod.Get, "https://graph.facebook.com/v21.0/me?fields=id,name", null, null);

    protected override (HttpMethod, string, string?, IReadOnlyDictionary<string, string>?)? LivePublish(PlatformConnection connection, string title, string body) =>
        (HttpMethod.Post, "https://graph.facebook.com/v21.0/me/feed", JsonSerializer.Serialize(new { message = $"{title}\n\n{SocialDraft.Parse(body).Text}" }), null);

    protected override async Task<PlatformPublishResult?> SendOfficialPublishAsync(
        PlatformConnection connection,
        string title,
        string body,
        PlatformPublishMedia media,
        CancellationToken cancellationToken)
    {
        if (Gateway is null || !media.HasImage && !media.HasVideo)
        {
            return null;
        }

        var caption = $"{title}\n\n{SocialDraft.Parse(body).Text}".Trim();
        var parts = new List<OfficialFormPart> { new("caption", null, "text/plain", System.Text.Encoding.UTF8.GetBytes(caption)) };
        string url;
        if (media.HasVideo)
        {
            url = "https://graph.facebook.com/v21.0/me/videos";
            if (media.VideoBytes is { Length: > 0 })
            {
                parts.Add(new("source", media.VideoFileName ?? "clip.mp4", "video/mp4", media.VideoBytes));
            }
            else if (!string.IsNullOrWhiteSpace(media.VideoUrl))
            {
                parts.Add(new("file_url", null, "text/plain", System.Text.Encoding.UTF8.GetBytes(media.VideoUrl)));
            }
        }
        else
        {
            url = "https://graph.facebook.com/v21.0/me/photos";
            if (media.ImageBytes is { Length: > 0 })
            {
                parts.Add(new("source", media.ImageFileName ?? "photo.jpg", "image/jpeg", media.ImageBytes));
            }
            else if (!string.IsNullOrWhiteSpace(media.ImageUrl))
            {
                parts.Add(new("url", null, "text/plain", System.Text.Encoding.UTF8.GetBytes(media.ImageUrl)));
            }
        }

        var result = await Gateway.SendMultipartAsync(HttpMethod.Post, url, connection.AccessToken, parts, cancellationToken);
        return Interpret(result, "Facebook");
    }

    protected override (HttpMethod, string, string?, IReadOnlyDictionary<string, string>?)? LiveMetrics(PlatformConnection connection) =>
        (HttpMethod.Get, "https://graph.facebook.com/v21.0/me?fields=id,name,fan_count", null, null);
}

public sealed class InstagramAdapter(IOfficialPlatformGateway gateway, ILiveTokenRefresher? tokens = null) : PlatformAdapter("INSTAGRAM", "Instagram", "Social", PlatformAuthMode.OAuth,
    "Professional account through official Meta authorization.",
    new(true, true, true, false, true, true, false), gateway, tokens)
{
    protected override (HttpMethod, string, string?, IReadOnlyDictionary<string, string>?)? LiveHealth(PlatformConnection connection) =>
        (HttpMethod.Get, "https://graph.facebook.com/v21.0/me/accounts", null, null);

    protected override async Task<PlatformPublishResult?> SendOfficialPublishAsync(
        PlatformConnection connection,
        string title,
        string body,
        PlatformPublishMedia media,
        CancellationToken cancellationToken)
    {
        var image = media.ImageUrl;
        if (Gateway is null || string.IsNullOrWhiteSpace(image))
        {
            return null;
        }

        var account = connection.ExternalAccount is { Length: > 0 } id && !id.Equals("INSTAGRAM", StringComparison.OrdinalIgnoreCase)
            ? id
            : "me";
        var caption = $"{title}\n\n{SocialDraft.Parse(body).Text}".Trim();
        var created = await Gateway.SendAsync(
            HttpMethod.Post,
            $"https://graph.facebook.com/v21.0/{account}/media",
            connection.AccessToken,
            JsonSerializer.Serialize(new { image_url = image, caption }),
            null,
            cancellationToken);
        if (!created.Ok)
        {
            return Interpret(created, "Instagram");
        }

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(created.Body) ? "{}" : created.Body);
        var creationId = doc.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
        if (string.IsNullOrWhiteSpace(creationId))
        {
            return new PlatformPublishResult("Hold", "Instagram accepted the container but did not return a creation id.");
        }

        var published = await Gateway.SendAsync(
            HttpMethod.Post,
            $"https://graph.facebook.com/v21.0/{account}/media_publish",
            connection.AccessToken,
            JsonSerializer.Serialize(new { creation_id = creationId }),
            null,
            cancellationToken);
        return Interpret(published, "Instagram");
    }
}

public sealed class LinkedInAdapter(IOfficialPlatformGateway gateway, ILiveTokenRefresher? tokens = null) : PlatformAdapter("LINKEDIN", "LinkedIn", "Social", PlatformAuthMode.OAuth,
    "Company page through official LinkedIn authorization.",
    new(true, true, false, false, true, true, false), gateway, tokens)
{
    protected override (HttpMethod, string, string?, IReadOnlyDictionary<string, string>?)? LiveHealth(PlatformConnection connection) =>
        (HttpMethod.Get, "https://api.linkedin.com/v2/userinfo", null, null);

    protected override (HttpMethod, string, string?, IReadOnlyDictionary<string, string>?)? LivePublish(
        PlatformConnection connection, string title, string body, PlatformPublishMedia? media)
    {
        var person = connection.ExternalAccount is { Length: > 0 } account &&
                     !account.Equals("LINKEDIN", StringComparison.OrdinalIgnoreCase)
            ? (account.StartsWith("urn:", StringComparison.Ordinal) ? account : $"urn:li:person:{account}")
            : "urn:li:person:me";
        var draft = SocialDraft.Parse(body);
        var image = media?.ImageUrl ?? draft.ImageUrl;
        var video = media?.VideoUrl ?? draft.VideoUrl;
        var share = new Dictionary<string, object?>
        {
            ["shareCommentary"] = new Dictionary<string, string> { ["text"] = $"{title}\n\n{draft.Text}".Trim() },
            ["shareMediaCategory"] = "NONE"
        };
        if (!string.IsNullOrWhiteSpace(image) || !string.IsNullOrWhiteSpace(video))
        {
            var source = !string.IsNullOrWhiteSpace(video) ? video : image;
            share["shareMediaCategory"] = string.IsNullOrWhiteSpace(video) ? "IMAGE" : "VIDEO";
            share["media"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["status"] = new Dictionary<string, string> { ["code"] = "READY" },
                    ["originalUrl"] = source
                }
            };
        }

        var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["author"] = person,
            ["lifecycleState"] = "PUBLISHED",
            ["specificContent"] = new Dictionary<string, object?>
            {
                ["com.linkedin.ugc.ShareContent"] = share
            },
            ["visibility"] = new Dictionary<string, string>
            {
                ["com.linkedin.ugc.MemberNetworkVisibility"] = "PUBLIC"
            }
        });
        return (HttpMethod.Post, "https://api.linkedin.com/v2/ugcPosts", payload, null);
    }
}

public sealed class YouTubeAdapter(IOfficialPlatformGateway gateway, ILiveTokenRefresher? tokens = null) : PlatformAdapter("YOUTUBE", "YouTube", "Social", PlatformAuthMode.OAuth,
    "Channel presence through official Google authorization.",
    new(true, true, false, false, true, true, false), gateway, tokens)
{
    protected override (HttpMethod, string, string?, IReadOnlyDictionary<string, string>?)? LiveHealth(PlatformConnection connection) =>
        (HttpMethod.Get, "https://www.googleapis.com/youtube/v3/channels?part=snippet,statistics&mine=true", null, null);

    protected override (HttpMethod, string, string?, IReadOnlyDictionary<string, string>?)? LiveMetrics(PlatformConnection connection) =>
        LiveHealth(connection);

    protected override async Task<PlatformPublishResult?> SendOfficialPublishAsync(
        PlatformConnection connection,
        string title,
        string body,
        PlatformPublishMedia media,
        CancellationToken cancellationToken)
    {
        if (Gateway is null || media.VideoBytes is not { Length: > 0 })
        {
            return null;
        }

        var draft = SocialDraft.Parse(body);
        var snippet = JsonSerializer.Serialize(new
        {
            snippet = new { title, description = draft.Text },
            status = new { privacyStatus = "unlisted" }
        });
        var parts = new List<OfficialFormPart>
        {
            new("snippet", "snippet.json", "application/json", System.Text.Encoding.UTF8.GetBytes(snippet)),
            new("video", media.VideoFileName ?? "video.mp4", "video/mp4", media.VideoBytes)
        };
        var result = await Gateway.SendMultipartAsync(
            HttpMethod.Post,
            "https://www.googleapis.com/upload/youtube/v3/videos?part=snippet,status&uploadType=multipart",
            connection.AccessToken,
            parts,
            cancellationToken);
        return Interpret(result, "YouTube");
    }
}

public sealed class IndiaMartAdapter(IOfficialPlatformGateway gateway, ILiveTokenRefresher? tokens = null) : PlatformAdapter("INDIAMART", "IndiaMART", "Directory", PlatformAuthMode.Assisted,
    "Official CRM lead pull when a seller CRM key is configured. Profile writes stay assisted. Unofficial APIs are out of scope.",
    new(true, false, false, false, false, true, true), gateway, tokens)
{
    protected override bool UseBearerToken => false;

    protected override (HttpMethod, string, string?, IReadOnlyDictionary<string, string>?)? LiveHealth(PlatformConnection connection)
    {
        if (string.IsNullOrWhiteSpace(connection.AccessToken))
        {
            return null;
        }

        var start = DateTime.UtcNow.AddDays(-1).ToString("dd-MMM-yyyy");
        var end = DateTime.UtcNow.ToString("dd-MMM-yyyy");
        var url = $"https://mapi.indiamart.com/wservce/crm/crmListing/v2/?glusr_crm_key={Uri.EscapeDataString(connection.AccessToken)}&start_time={start}&end_time={end}";
        return (HttpMethod.Get, url, null, null);
    }

    protected override (HttpMethod, string, string?, IReadOnlyDictionary<string, string>?)? LiveMetrics(PlatformConnection connection) =>
        LiveHealth(connection);
}

public sealed class JustdialAdapter() : PlatformAdapter("JUSTDIAL", "Justdial", "Directory", PlatformAuthMode.Assisted,
    "Assisted workflow. Justdial has no public official write API in this catalog.",
    new(true, false, false, false, false, false, true));

public sealed class WhatsAppAdapter(IOfficialPlatformGateway gateway, ILiveTokenRefresher? tokens = null) : PlatformAdapter("WHATSAPP", "WhatsApp", "Messaging", PlatformAuthMode.ApiKey,
    "WhatsApp Business Platform / Cloud API only. Unofficial clients are out of scope.",
    new(false, true, false, false, true, true, false), gateway, tokens)
{
    protected override (HttpMethod, string, string?, IReadOnlyDictionary<string, string>?)? LiveHealth(PlatformConnection connection) =>
        (HttpMethod.Get, "https://graph.facebook.com/v21.0/me", null, null);
}

public sealed class WebsiteAdapter() : PlatformAdapter("WEBSITE", "Website", "Website", PlatformAuthMode.Assisted,
    "Official site used as the identity comparison source. CMS adapters come later.",
    new(true, false, false, false, false, false, true));

public sealed class SearchConsoleAdapter(IOfficialPlatformGateway gateway, ILiveTokenRefresher? tokens = null) : PlatformAdapter("SEARCH_CONSOLE", "Search Console", "Search", PlatformAuthMode.OAuth,
    "Search visibility through official Search Console authorization.",
    new(true, false, true, false, false, true, false), gateway, tokens)
{
    protected override (HttpMethod, string, string?, IReadOnlyDictionary<string, string>?)? LiveHealth(PlatformConnection connection) =>
        (HttpMethod.Get, "https://searchconsole.googleapis.com/webmasters/v3/sites", null, null);
}

public sealed class GoogleAdsAdapter(IOfficialPlatformGateway gateway, IConfiguration configuration, ILiveTokenRefresher? tokens = null) : PlatformAdapter("GOOGLE_ADS", "Google Ads", "Ads", PlatformAuthMode.OAuth,
    "Advertising account through official Google Ads authorization. Campaigns are not mutated.",
    new(true, false, false, false, false, true, false), gateway, tokens)
{
    protected override (HttpMethod, string, string?, IReadOnlyDictionary<string, string>?)? LiveHealth(PlatformConnection connection) =>
        GoogleAdsHeaders() is { } headers
            ? (HttpMethod.Get, "https://googleads.googleapis.com/v17/customers:listAccessibleCustomers", null, headers)
            : null;

    public override async Task<PlatformMetricsResult> MetricsAsync(PlatformConnection connection, CancellationToken cancellationToken)
    {
        await EnsureFreshAsync(connection, cancellationToken);
        if (!connection.HasLiveCredential || Gateway is null)
        {
            return new PlatformMetricsResult("Hold", "Live Google Ads metrics wait for an official OAuth grant. Campaigns are not invented.");
        }

        var headers = GoogleAdsHeaders();
        if (headers is null)
        {
            return new PlatformMetricsResult("Hold", "Connections:GoogleAds:DeveloperToken is not configured. Campaigns are not invented.");
        }

        var customers = await Gateway.SendAsync(
            HttpMethod.Get,
            "https://googleads.googleapis.com/v17/customers:listAccessibleCustomers",
            connection.AccessToken,
            null,
            headers,
            cancellationToken);
        if (!customers.Ok)
        {
            return new PlatformMetricsResult("Hold", $"Official Google Ads customer list returned {customers.StatusCode}. Campaigns were not invented.");
        }

        var ids = DigitalPulse.Application.Website.GoogleAdsCampaigns.CustomerIds(customers.Body);
        if (ids.Count == 0)
        {
            return new PlatformMetricsResult("Observed", """{"results":[]}""");
        }

        var preferred = configuration["Connections:GoogleAds:LoginCustomerId"]?.Trim();
        var customerId = !string.IsNullOrWhiteSpace(preferred) && ids.Contains(preferred)
            ? preferred
            : connection.ExternalAccount is { Length: > 0 } account &&
              ids.Contains(account.Replace("customers/", string.Empty, StringComparison.OrdinalIgnoreCase))
                ? account.Replace("customers/", string.Empty, StringComparison.OrdinalIgnoreCase)
                : ids[0];
        var query = JsonSerializer.Serialize(new
        {
            query = "SELECT campaign.id, campaign.name, campaign.status, metrics.impressions, metrics.clicks FROM campaign WHERE segments.date DURING LAST_7_DAYS ORDER BY metrics.impressions DESC LIMIT 10"
        });
        var search = await Gateway.SendAsync(
            HttpMethod.Post,
            $"https://googleads.googleapis.com/v17/customers/{customerId}/googleAds:search",
            connection.AccessToken,
            query,
            headers,
            cancellationToken);
        if (!search.Ok)
        {
            return new PlatformMetricsResult("Hold", $"Official Google Ads search returned {search.StatusCode}. Campaigns were not invented.");
        }

        return new PlatformMetricsResult("Observed", search.Body);
    }

    private IReadOnlyDictionary<string, string>? GoogleAdsHeaders()
    {
        var token = configuration["Connections:GoogleAds:DeveloperToken"];
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var headers = new Dictionary<string, string> { ["developer-token"] = token.Trim() };
        var login = configuration["Connections:GoogleAds:LoginCustomerId"];
        if (!string.IsNullOrWhiteSpace(login))
        {
            headers["login-customer-id"] = login.Trim();
        }

        return headers;
    }
}

public sealed class GoogleAnalyticsAdapter(IOfficialPlatformGateway gateway, ILiveTokenRefresher? tokens = null) : PlatformAdapter("GOOGLE_ANALYTICS", "Google Analytics", "Analytics", PlatformAuthMode.OAuth,
    "GA4 property through official Google Analytics authorization. Sessions are not invented. Measurement Protocol hits are out of scope.",
    new(true, false, false, false, false, true, false), gateway, tokens)
{
    protected override (HttpMethod, string, string?, IReadOnlyDictionary<string, string>?)? LiveHealth(PlatformConnection connection) =>
        (HttpMethod.Get, "https://analyticsadmin.googleapis.com/v1beta/accountSummaries", null, null);

    protected override (HttpMethod, string, string?, IReadOnlyDictionary<string, string>?)? LiveMetrics(PlatformConnection connection)
    {
        if (string.IsNullOrWhiteSpace(connection.ExternalAccount)
            || connection.ExternalAccount.Equals("GOOGLE_ANALYTICS", StringComparison.OrdinalIgnoreCase)
            || !connection.ExternalAccount.StartsWith("properties/", StringComparison.OrdinalIgnoreCase))
        {
            return (HttpMethod.Get, "https://analyticsadmin.googleapis.com/v1beta/accountSummaries", null, null);
        }

        var body = JsonSerializer.Serialize(new
        {
            dateRanges = new[] { new { startDate = "7daysAgo", endDate = "today" } },
            metrics = new[] { new { name = "sessions" } }
        });
        return (HttpMethod.Post, $"https://analyticsdata.googleapis.com/v1beta/{connection.ExternalAccount}:runReport", body, null);
    }
}

public sealed class PlatformAdapterCatalog : IPlatformAdapterCatalog
{
    private readonly Dictionary<string, IPlatformAdapter> _adapters;

    public PlatformAdapterCatalog(IEnumerable<IPlatformAdapter> adapters) =>
        _adapters = adapters.ToDictionary(a => a.Describe().Code, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<IPlatformAdapter> All() => _adapters.Values.OrderBy(a => a.Describe().Name).ToList();

    public IPlatformAdapter Get(string code) =>
        _adapters.TryGetValue(code.Trim(), out var adapter)
            ? adapter
            : throw AppException.NotFound($"Unknown platform '{code}'.");
}
