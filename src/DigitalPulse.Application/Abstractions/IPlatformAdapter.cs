using DigitalPulse.Domain.Platforms;

namespace DigitalPulse.Application.Abstractions;

public sealed record PlatformDescriptor(
    string Code,
    string Name,
    string Category,
    PlatformAuthMode AuthMode,
    string Summary,
    PlatformCapabilities Capabilities);

public sealed record PlatformHealthResult(string Status, string Detail);
public sealed record PlatformDiagnostic(string Check, string Status, string Detail);
public sealed record PlatformPublishResult(string Status, string Detail);
public sealed record PlatformMetricsResult(string Status, string Detail);

public interface IPlatformAdapter
{
    PlatformDescriptor Describe();
    Task<PlatformHealthResult> HealthCheckAsync(PlatformConnection connection, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlatformDiagnostic>> DiagnoseAsync(PlatformConnection connection, CancellationToken cancellationToken);
    Task<PlatformPublishResult> PublishAsync(PlatformConnection connection, string title, string body, CancellationToken cancellationToken);
    Task<PlatformMetricsResult> MetricsAsync(PlatformConnection connection, CancellationToken cancellationToken);
}

public interface IPlatformAdapterCatalog
{
    IReadOnlyList<IPlatformAdapter> All();
    IPlatformAdapter Get(string code);
}

public sealed record AuthorizationStart(string AuthorizationUrl, bool CompleteInPlace);

public interface IPlatformAuthorizationBroker
{
    Task<AuthorizationStart> StartAsync(PlatformConnection connection, IPlatformAdapter adapter, CancellationToken cancellationToken);
    Task CompleteAsync(PlatformConnection connection, string? code, CancellationToken cancellationToken);
}

public interface ILiveTokenRefresher
{
    Task EnsureFreshAsync(PlatformConnection connection, CancellationToken cancellationToken);
}
