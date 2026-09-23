using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Tenancy;

public sealed class Tenant : Entity
{
    public string Name { get; private set; } = string.Empty;
    public TenantType Type { get; private set; }

    private Tenant() { }

    public static Tenant Create(string name, TenantType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (type == TenantType.Platform)
        {
            throw new InvalidOperationException("Platform tenants cannot be created through self-serve onboarding.");
        }

        return new Tenant
        {
            Name = name.Trim(),
            Type = type
        };
    }

    public static Tenant CreatePlatform(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Tenant
        {
            Name = name.Trim(),
            Type = TenantType.Platform
        };
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Touch();
    }
}
