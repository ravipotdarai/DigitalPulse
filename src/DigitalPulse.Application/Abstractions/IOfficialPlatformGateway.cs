using DigitalPulse.Domain.Platforms;

namespace DigitalPulse.Application.Abstractions;

public sealed record OfficialHttpResult(int StatusCode, string Body, bool Ok);

public interface IOfficialPlatformGateway
{
    Task<OfficialHttpResult> SendAsync(
        HttpMethod method,
        string url,
        string? accessToken,
        string? jsonBody,
        IReadOnlyDictionary<string, string>? headers,
        CancellationToken cancellationToken);
}
