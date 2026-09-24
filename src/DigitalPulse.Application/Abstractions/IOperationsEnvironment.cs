namespace DigitalPulse.Application.Abstractions;

public interface IOperationsEnvironment
{
    string EnvironmentName { get; }
    string HostRole { get; }
    bool RateLimitingEnabled { get; }
    int RateLimitPerMinute { get; }
    bool KeyVaultConfigured { get; }
    bool AppInsightsConfigured { get; }
    bool RedisConfigured { get; }
    bool AzureBackupConfigured { get; }
    bool LiveBillingConfigured { get; }
    bool LiveAiConfigured { get; }
}

public sealed record PackageInventory(IReadOnlyList<string> Packages, IReadOnlyList<string> ContainerBases, string DependencyHold, string ContainerHold);

public interface IPackageInventory
{
    PackageInventory Collect();
}
