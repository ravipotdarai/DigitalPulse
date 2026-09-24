using DigitalPulse.Application.Abstractions;
using DigitalPulse.Domain.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DigitalPulse.Infrastructure.Operations;

public sealed class ConfigurationOperationsEnvironment : IOperationsEnvironment
{
    public ConfigurationOperationsEnvironment(IConfiguration configuration, IHostEnvironment host)
    {
        EnvironmentName = host.EnvironmentName;
        HostRole = configuration["Operations:HostRole"] ?? "Api";
        RateLimitingEnabled = !configuration.GetValue<bool>("Testing:UseInMemory");
        RateLimitPerMinute = configuration.GetValue("Operations:RateLimitPerMinute", OperationsPolicy.RateLimitPerMinute);
        KeyVaultConfigured = Has(configuration["Operations:KeyVaultUri"]);
        AppInsightsConfigured = Has(configuration["Operations:ApplicationInsightsConnectionString"]);
        RedisConfigured = Has(configuration["Operations:Redis"]);
        AzureBackupConfigured = Has(configuration["Operations:AzureBackup"]);
        LiveBillingConfigured = Has(configuration["Billing:Razorpay:KeyId"]) && Has(configuration["Billing:Razorpay:KeySecret"]);
        LiveAiConfigured = Has(configuration["Ai:OpenAi:ApiKey"]);
    }

    public string EnvironmentName { get; }
    public string HostRole { get; }
    public bool RateLimitingEnabled { get; }
    public int RateLimitPerMinute { get; }
    public bool KeyVaultConfigured { get; }
    public bool AppInsightsConfigured { get; }
    public bool RedisConfigured { get; }
    public bool AzureBackupConfigured { get; }
    public bool LiveBillingConfigured { get; }
    public bool LiveAiConfigured { get; }

    private static bool Has(string? value) => !string.IsNullOrWhiteSpace(value);
}

public sealed class FilePackageInventory : IPackageInventory
{
    public PackageInventory Collect()
    {
        var root = FindRepoRoot();
        var packages = new List<string>();
        var images = new List<string>();
        if (root is not null)
        {
            var props = Path.Combine(root, "Directory.Packages.props");
            if (File.Exists(props))
            {
                packages.AddRange(ReadIncludes(File.ReadAllText(props), "Include=\"", "\""));
            }

            var dockerfile = Path.Combine(root, "Dockerfile");
            if (File.Exists(dockerfile))
            {
                foreach (var line in File.ReadAllLines(dockerfile))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("FROM ", StringComparison.OrdinalIgnoreCase))
                    {
                        images.Add(trimmed[5..].Trim());
                    }
                }
            }
        }

        return new PackageInventory(
            packages,
            images,
            packages.Count == 0 ? "Project files were not mounted. Package inventory stays held and CVEs are not invented." : OperationsPolicy.AdvisoryHold,
            images.Count == 0 ? "No Dockerfile was found. Container scanning stays held." : OperationsPolicy.ContainerHold);
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DigitalPulse.slnx")) ||
                File.Exists(Path.Combine(dir.FullName, "DigitalPulse.sln")) ||
                File.Exists(Path.Combine(dir.FullName, "Directory.Packages.props")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }

    private static IEnumerable<string> ReadIncludes(string text, string start, string end)
    {
        var index = 0;
        while (index < text.Length)
        {
            var at = text.IndexOf(start, index, StringComparison.Ordinal);
            if (at < 0)
            {
                yield break;
            }

            at += start.Length;
            var close = text.IndexOf(end, at, StringComparison.Ordinal);
            if (close < 0)
            {
                yield break;
            }

            var value = text[at..close];
            if (!value.Contains('\\') && !value.Contains('/') && value.Contains('.'))
            {
                yield return value;
            }

            index = close + end.Length;
        }
    }
}
