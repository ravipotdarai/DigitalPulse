using System.Net;
using System.Net.Sockets;
using System.Text;
using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;

namespace DigitalPulse.Infrastructure.Scanning;

public sealed class StubWebsiteFetcher : IWebsiteFetcher
{
    public const string FixtureHtml =
        """
        <!doctype html>
        <html>
        <head><meta name="viewport" content="width=device-width"></head>
        <body><p>Welcome.</p></body>
        </html>
        """;

    public Task<WebsiteFetchResult> FetchAsync(string? website, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(website))
        {
            return Task.FromResult(WebsiteFetchResult.NotAttempted());
        }

        if (!SafeUrlPolicy.TryValidate(website, out var uri, out var error))
        {
            return Task.FromResult(WebsiteFetchResult.BlockedUrl(error));
        }

        return Task.FromResult(WebsiteFetchResult.Ok(uri.ToString(), 200, FixtureHtml));
    }
}

public sealed class WebsiteFetcher : IWebsiteFetcher
{
    private const int MaxBytes = 256 * 1024;
    private readonly HttpClient _http;

    public WebsiteFetcher(HttpClient http) => _http = http;

    public async Task<WebsiteFetchResult> FetchAsync(string? website, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(website))
        {
            return WebsiteFetchResult.NotAttempted();
        }

        if (!SafeUrlPolicy.TryValidate(website, out var uri, out var error))
        {
            return WebsiteFetchResult.BlockedUrl(error);
        }

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(uri.IdnHost, cancellationToken);
            if (addresses.Length == 0 || addresses.Any(a => !SafeUrlPolicy.IsPublicAddress(a)))
            {
                return WebsiteFetchResult.BlockedUrl("Website host resolves to a private or reserved address.");
            }
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            return WebsiteFetchResult.Failed(uri.ToString(), "Website host could not be resolved.");
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
                        return WebsiteFetchResult.Failed(current.ToString(), "Redirect had no Location header.");
                    }

                    var next = location.IsAbsoluteUri ? location : new Uri(current, location);
                    if (!SafeUrlPolicy.TryValidate(next.ToString(), out current, out var redirectError))
                    {
                        return WebsiteFetchResult.BlockedUrl(redirectError);
                    }

                    continue;
                }

                var html = await ReadLimitedAsync(response, cancellationToken);
                return WebsiteFetchResult.Ok(current.ToString(), (int)response.StatusCode, html);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                return WebsiteFetchResult.Failed(current.ToString(), "Website did not return a usable public response.");
            }
        }

        return WebsiteFetchResult.Failed(uri.ToString(), "Too many redirects.");
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
}
