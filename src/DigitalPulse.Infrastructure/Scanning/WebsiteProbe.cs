using System.Net;
using System.Net.Sockets;
using System.Text;
using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;

namespace DigitalPulse.Infrastructure.Scanning;

public sealed class StubWebsiteProbe : IWebsiteProbe
{
    public Task<WebsiteProbeResult> ProbeAsync(
        string? website,
        string businessName,
        IReadOnlyList<string> phones,
        CancellationToken cancellationToken) =>
        Task.FromResult(WebsiteProbeResult.DisabledInHost());
}

public sealed class WebsiteProbe : IWebsiteProbe
{
    private const int MaxBytes = 256 * 1024;
    private readonly HttpClient _http;

    public WebsiteProbe(HttpClient http) => _http = http;

    public async Task<WebsiteProbeResult> ProbeAsync(
        string? website,
        string businessName,
        IReadOnlyList<string> phones,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(website))
        {
            return WebsiteProbeResult.NotAttempted();
        }

        if (!SafeUrlPolicy.TryValidate(website, out var uri, out var error))
        {
            return WebsiteProbeResult.BlockedUrl(error);
        }

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(uri.IdnHost, cancellationToken);
            if (addresses.Length == 0 || addresses.Any(a => !SafeUrlPolicy.IsPublicAddress(a)))
            {
                return WebsiteProbeResult.BlockedUrl("Website host resolves to a private or reserved address.");
            }
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            return WebsiteProbeResult.Failed(uri.ToString(), "Website host could not be resolved.");
        }

        var current = uri;
        for (var hop = 0; hop < 3; hop++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            try
            {
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if ((int)response.StatusCode is >= 300 and < 400)
                {
                    var location = response.Headers.Location;
                    if (location is null)
                    {
                        return WebsiteProbeResult.Failed(current.ToString(), "Redirect had no Location header.");
                    }

                    var next = location.IsAbsoluteUri ? location : new Uri(current, location);
                    if (!SafeUrlPolicy.TryValidate(next.ToString(), out current, out var redirectError))
                    {
                        return WebsiteProbeResult.BlockedUrl(redirectError);
                    }

                    continue;
                }

                var body = await ReadLimitedAsync(response, cancellationToken);
                var nameHit = !string.IsNullOrWhiteSpace(businessName)
                    && body.Contains(businessName, StringComparison.OrdinalIgnoreCase);
                var phoneHit = phones.Any(phone => ContainsPhone(body, phone));
                return WebsiteProbeResult.Ok(current.ToString(), (int)response.StatusCode, nameHit, phoneHit, body.Length);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                return WebsiteProbeResult.Failed(current.ToString(), "Website did not return a usable public response.");
            }
        }

        return WebsiteProbeResult.Failed(uri.ToString(), "Too many redirects.");
    }

    private static async Task<string> ReadLimitedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var buffer = new char[4096];
        var built = new StringBuilder();
        while (built.Length < MaxBytes)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, MaxBytes - built.Length)), cancellationToken);
            if (read <= 0)
            {
                break;
            }

            built.Append(buffer, 0, read);
        }

        return built.ToString();
    }

    private static bool ContainsPhone(string body, string phone)
    {
        var expected = Digits(phone);
        return expected.Length >= 8 && Digits(body).Contains(expected, StringComparison.Ordinal);
    }

    private static string Digits(string value) =>
        new(value.Where(char.IsDigit).ToArray());
}
