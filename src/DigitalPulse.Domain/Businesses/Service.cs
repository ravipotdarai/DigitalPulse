using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Businesses;

public sealed class Service : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private Service() { }

    public static Service Create(Guid tenantId, Guid businessId, string name, string? description)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Service
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Name = name.Trim(),
            Description = NullIfEmpty(description)
        };
    }

    public void Update(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = NullIfEmpty(description);
        Touch();
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
