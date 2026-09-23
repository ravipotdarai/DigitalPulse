using DigitalPulse.Application.Abstractions;
using DigitalPulse.Domain.Platforms;

namespace DigitalPulse.Infrastructure.Platforms;

public sealed class DevelopmentAuthorizationBroker : IPlatformAuthorizationBroker
{
    public Task<AuthorizationStart> StartAsync(
        PlatformConnection connection,
        IPlatformAdapter adapter,
        CancellationToken cancellationToken)
    {
        var mode = adapter.Describe().AuthMode;
        if (mode == PlatformAuthMode.Assisted)
        {
            return Task.FromResult(new AuthorizationStart(string.Empty, true));
        }

        var url = $"/v1/connections/callback?state={Uri.EscapeDataString(connection.AuthorizationState!)}&code=development";
        return Task.FromResult(new AuthorizationStart(url, false));
    }

    public Task CompleteAsync(PlatformConnection connection, string? code, CancellationToken cancellationToken)
    {
        if (connection.AuthMode != PlatformAuthMode.Assisted &&
            !string.Equals(code, "development", StringComparison.OrdinalIgnoreCase))
        {
            connection.MarkError("Authorization code was not a development grant.");
            return Task.CompletedTask;
        }

        var account = connection.AuthMode == PlatformAuthMode.Assisted
            ? "Assisted workspace"
            : $"Development · {connection.PlatformCode}";
        connection.MarkConnected("Development", Guid.NewGuid().ToString("N"), account);
        return Task.CompletedTask;
    }
}
