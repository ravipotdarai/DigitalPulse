using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Businesses;

public sealed class Brand : TenantOwnedEntity
{
    public string Name { get; private set; } = string.Empty;

    private Brand() { }

    public static Brand Create(Guid tenantId, string name)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Brand
        {
            TenantId = tenantId,
            Name = name.Trim()
        };
    }
}

public sealed class BusinessBrand : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public Guid BrandId { get; private set; }

    private BusinessBrand() { }

    public static BusinessBrand Link(Guid tenantId, Guid businessId, Guid brandId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        if (brandId == Guid.Empty) throw new ArgumentException("Brand is required.", nameof(brandId));

        return new BusinessBrand
        {
            TenantId = tenantId,
            BusinessId = businessId,
            BrandId = brandId
        };
    }
}
