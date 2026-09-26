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

        return Task.FromResult(new AuthorizationStart(string.Empty, false));
    }

    public Task CompleteAsync(PlatformConnection connection, string? code, CancellationToken cancellationToken)
    {
        if (connection.AuthMode == PlatformAuthMode.Assisted)
        {
            connection.MarkConnected("Assisted", "assisted", "Assisted workspace");
            return Task.CompletedTask;
        }

        connection.MarkError("Sign in on the official platform to connect. A development grant is not accepted.");
        return Task.CompletedTask;
    }
}
