using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Businesses;

public sealed class BusinessCategory : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private BusinessCategory() { }

    public static BusinessCategory Create(Guid tenantId, Guid businessId, string name)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new BusinessCategory
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Name = name.Trim()
        };
    }

    public void Update(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Touch();
    }
}
