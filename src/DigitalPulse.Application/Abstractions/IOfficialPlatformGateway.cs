using DigitalPulse.Domain.Platforms;

namespace DigitalPulse.Application.Abstractions;

public sealed record OfficialHttpResult(int StatusCode, string Body, bool Ok);

public sealed record OfficialFormPart(string Name, string? FileName, string ContentType, byte[] Bytes);

public interface IOfficialPlatformGateway
{
    Task<OfficialHttpResult> SendAsync(
        HttpMethod method,
        string url,
        string? accessToken,
        string? jsonBody,
        IReadOnlyDictionary<string, string>? headers,
        CancellationToken cancellationToken);

    Task<OfficialHttpResult> SendMultipartAsync(
        HttpMethod method,
        string url,
        string? accessToken,
        IReadOnlyList<OfficialFormPart> parts,
        CancellationToken cancellationToken);
}
