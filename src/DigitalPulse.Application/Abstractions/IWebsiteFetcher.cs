namespace DigitalPulse.Application.Abstractions;

public sealed record WebsiteFetchResult(
    bool Attempted,
    bool Disabled,
    bool Blocked,
    bool Reached,
    string? FinalUrl,
    int? StatusCode,
    string? Error,
    string? Html)
{
    public static WebsiteFetchResult NotAttempted() =>
        new(false, false, false, false, null, null, null, null);

    public static WebsiteFetchResult DisabledInHost() =>
        new(false, true, false, false, null, null, "Website fetch is disabled in this host.", null);

    public static WebsiteFetchResult BlockedUrl(string error) =>
        new(true, false, true, false, null, null, error, null);

    public static WebsiteFetchResult Failed(string? url, string error) =>
        new(true, false, false, false, url, null, error, null);

    public static WebsiteFetchResult Ok(string url, int statusCode, string html) =>
        new(true, false, false, true, url, statusCode, null, html);
}

public interface IWebsiteFetcher
{
    Task<WebsiteFetchResult> FetchAsync(string? website, CancellationToken cancellationToken);
}
