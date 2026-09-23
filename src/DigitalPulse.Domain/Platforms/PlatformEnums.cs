namespace DigitalPulse.Domain.Platforms;

public enum PlatformAuthMode
{
    OAuth = 1,
    ApiKey = 2,
    Assisted = 3
}

public enum ConnectionStatus
{
    Connecting = 1,
    Connected = 2,
    NeedsReauth = 3,
    Error = 4
}

public sealed record PlatformCapabilities(
    bool CanRead,
    bool CanCreate,
    bool CanUpdate,
    bool CanDelete,
    bool CanPublish,
    bool CanGetMetrics,
    bool AssistedOnly);
