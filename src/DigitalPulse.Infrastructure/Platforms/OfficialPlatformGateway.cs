using System.Net.Http.Headers;
using System.Text;
using DigitalPulse.Application.Abstractions;

namespace DigitalPulse.Infrastructure.Platforms;

public sealed class OfficialPlatformGateway : IOfficialPlatformGateway
{
    private readonly IHttpClientFactory _http;

    public OfficialPlatformGateway(IHttpClientFactory http) => _http = http;

    public async Task<OfficialHttpResult> SendAsync(
        HttpMethod method,
        string url,
        string? accessToken,
        string? jsonBody,
        IReadOnlyDictionary<string, string>? headers,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (headers is not null)
        {
            foreach (var pair in headers)
            {
                request.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
            }
        }

        if (jsonBody is not null)
        {
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }

        using var response = await _http.CreateClient("official-platforms").SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return new OfficialHttpResult((int)response.StatusCode, body, response.IsSuccessStatusCode);
    }

    public async Task<OfficialHttpResult> SendMultipartAsync(
        HttpMethod method,
        string url,
        string? accessToken,
        IReadOnlyList<OfficialFormPart> parts,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var form = new MultipartFormDataContent();
        foreach (var part in parts)
        {
            var content = new ByteArrayContent(part.Bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue(part.ContentType);
            if (string.IsNullOrWhiteSpace(part.FileName))
            {
                form.Add(content, part.Name);
            }
            else
            {
                form.Add(content, part.Name, part.FileName);
            }
        }

        request.Content = form;
        using var response = await _http.CreateClient("official-platforms").SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return new OfficialHttpResult((int)response.StatusCode, body, response.IsSuccessStatusCode);
    }
}
