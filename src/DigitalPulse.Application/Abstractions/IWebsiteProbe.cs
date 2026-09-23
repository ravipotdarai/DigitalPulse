namespace DigitalPulse.Application.Abstractions;

public sealed record WebsiteProbeResult(
    bool Attempted,
    bool Disabled,
    bool Blocked,
    bool Reached,
    string? FinalUrl,
    int? StatusCode,
    string? Error,
    bool ContainsBusinessName,
    bool ContainsPhone,
    int BytesRead)
{
    public static WebsiteProbeResult NotAttempted() =>
        new(false, false, false, false, null, null, null, false, false, 0);

    public static WebsiteProbeResult DisabledInHost() =>
        new(false, true, false, false, null, null, "Website discovery is disabled in this host.", false, false, 0);

    public static WebsiteProbeResult BlockedUrl(string error) =>
        new(true, false, true, false, null, null, error, false, false, 0);

    public static WebsiteProbeResult Failed(string? url, string error) =>
        new(true, false, false, false, url, null, error, false, false, 0);

    public static WebsiteProbeResult Ok(string url, int statusCode, bool containsName, bool containsPhone, int bytesRead) =>
        new(true, false, false, true, url, statusCode, null, containsName, containsPhone, bytesRead);
}

public interface IWebsiteProbe
{
    Task<WebsiteProbeResult> ProbeAsync(
        string? website,
        string businessName,
        IReadOnlyList<string> phones,
        CancellationToken cancellationToken);
}
