using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Domain.Platforms;

namespace DigitalPulse.Infrastructure.Platforms;

public abstract class PlatformAdapter : IPlatformAdapter
{
    protected PlatformAdapter(
        string code,
        string name,
        string category,
        PlatformAuthMode authMode,
        string summary,
        PlatformCapabilities capabilities)
    {
        Descriptor = new PlatformDescriptor(code, name, category, authMode, summary, capabilities);
    }

    public PlatformDescriptor Descriptor { get; }
    public PlatformDescriptor Describe() => Descriptor;

    public Task<PlatformHealthResult> HealthCheckAsync(PlatformConnection connection, CancellationToken cancellationToken)
    {
        if (connection.Status == ConnectionStatus.Connected && connection.GrantKind == "Development")
        {
            return Task.FromResult(new PlatformHealthResult(
                "Healthy",
                "Development grant is present. Production provider APIs are not configured."));
        }

        if (connection.Status == ConnectionStatus.NeedsReauth)
        {
            return Task.FromResult(new PlatformHealthResult("NeedsReauth", connection.LastError ?? "Reauthorization is required."));
        }

        return Task.FromResult(new PlatformHealthResult("Error", connection.LastError ?? "The connection is not healthy."));
    }

    public Task<IReadOnlyList<PlatformDiagnostic>> DiagnoseAsync(PlatformConnection connection, CancellationToken cancellationToken)
    {
        IReadOnlyList<PlatformDiagnostic> checks =
        [
            new("Adapter", "Pass", $"{Descriptor.Name} adapter is registered."),
            new("Authorization mode", "Pass", Descriptor.AuthMode.ToString()),
            new("Grant", connection.GrantReference is null ? "Fail" : "Pass",
                connection.GrantKind is null ? "No grant stored." : $"{connection.GrantKind} grant. Access tokens are not stored in logs."),
            new("Live provider API", "Hold", "Official production OAuth/API is not wired. DigitalPulse will not invent a live integration."),
            new("Capabilities", "Pass", Summarize(Descriptor.Capabilities))
        ];
        return Task.FromResult(checks);
    }

    public Task<PlatformPublishResult> PublishAsync(PlatformConnection connection, string title, string body, CancellationToken cancellationToken)
    {
        _ = title;
        _ = body;
        if (Descriptor.Capabilities.AssistedOnly)
        {
            return Task.FromResult(new PlatformPublishResult("Assisted", "This adapter is assisted-only. Publish the copy on the platform yourself."));
        }

        if (!Descriptor.Capabilities.CanPublish)
        {
            return Task.FromResult(new PlatformPublishResult("Assisted", $"{Descriptor.Name} does not expose an official publish capability in this catalog."));
        }

        if (connection.Status != ConnectionStatus.Connected)
        {
            return Task.FromResult(new PlatformPublishResult("Blocked", "Connect the platform before attempting a publish."));
        }

        return Task.FromResult(new PlatformPublishResult(
            "Hold",
            "Live provider publish is not wired. DigitalPulse will not invent a posted update."));
    }

    public Task<PlatformMetricsResult> MetricsAsync(PlatformConnection connection, CancellationToken cancellationToken)
    {
        if (!Descriptor.Capabilities.CanGetMetrics)
        {
            return Task.FromResult(new PlatformMetricsResult("Unavailable", $"{Descriptor.Name} does not expose metrics in this catalog."));
        }

        if (connection.Status != ConnectionStatus.Connected)
        {
            return Task.FromResult(new PlatformMetricsResult("Unavailable", "Connect the platform before requesting metrics."));
        }

        return Task.FromResult(new PlatformMetricsResult(
            "Hold",
            "Live provider metrics are not wired. DigitalPulse will not invent likes, views, or reach."));
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
}

public sealed class GoogleAdapter() : PlatformAdapter("GOOGLE", "Google", "Search", PlatformAuthMode.OAuth,
    "Business Profile and Search presence through official Google authorization.",
    new(true, false, true, false, false, true, false));

public sealed class FacebookAdapter() : PlatformAdapter("FACEBOOK", "Facebook", "Social", PlatformAuthMode.OAuth,
    "Page presence through official Meta authorization.",
    new(true, true, true, false, true, true, false));

public sealed class InstagramAdapter() : PlatformAdapter("INSTAGRAM", "Instagram", "Social", PlatformAuthMode.OAuth,
    "Professional account through official Meta authorization.",
    new(true, true, true, false, true, true, false));

public sealed class LinkedInAdapter() : PlatformAdapter("LINKEDIN", "LinkedIn", "Social", PlatformAuthMode.OAuth,
    "Company page through official LinkedIn authorization.",
    new(true, true, false, false, true, true, false));

public sealed class YouTubeAdapter() : PlatformAdapter("YOUTUBE", "YouTube", "Social", PlatformAuthMode.OAuth,
    "Channel presence through official Google authorization.",
    new(true, true, false, false, true, true, false));

public sealed class IndiaMartAdapter() : PlatformAdapter("INDIAMART", "IndiaMART", "Directory", PlatformAuthMode.Assisted,
    "Assisted workflow. DigitalPulse will not invent an unofficial write API.",
    new(true, false, false, false, false, false, true));

public sealed class JustdialAdapter() : PlatformAdapter("JUSTDIAL", "Justdial", "Directory", PlatformAuthMode.Assisted,
    "Assisted workflow. DigitalPulse will not invent an unofficial write API.",
    new(true, false, false, false, false, false, true));

public sealed class WhatsAppAdapter() : PlatformAdapter("WHATSAPP", "WhatsApp", "Messaging", PlatformAuthMode.ApiKey,
    "WhatsApp Business Platform / Cloud API only. Unofficial clients are out of scope.",
    new(false, true, false, false, true, true, false));

public sealed class WebsiteAdapter() : PlatformAdapter("WEBSITE", "Website", "Website", PlatformAuthMode.Assisted,
    "Official site used as the identity comparison source. CMS adapters come later.",
    new(true, false, false, false, false, false, true));

public sealed class SearchConsoleAdapter() : PlatformAdapter("SEARCH_CONSOLE", "Search Console", "Search", PlatformAuthMode.OAuth,
    "Search visibility through official Search Console authorization.",
    new(true, false, false, false, false, true, false));

public sealed class GoogleAdsAdapter() : PlatformAdapter("GOOGLE_ADS", "Google Ads", "Ads", PlatformAuthMode.OAuth,
    "Advertising account through official Google Ads authorization.",
    new(true, false, false, false, false, true, false));

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
