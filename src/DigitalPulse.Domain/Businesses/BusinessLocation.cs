using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Businesses;

public sealed class BusinessLocation : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? AddressLine { get; private set; }
    public string? City { get; private set; }
    public string? Region { get; private set; }
    public string? PostalCode { get; private set; }
    public string CountryCode { get; private set; } = "IN";

    private BusinessLocation() { }

    public static BusinessLocation Create(
        Guid tenantId,
        Guid businessId,
        string name,
        string? addressLine,
        string? city,
        string? region,
        string? postalCode,
        string? countryCode)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new BusinessLocation
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Name = name.Trim(),
            AddressLine = NullIfEmpty(addressLine),
            City = NullIfEmpty(city),
            Region = NullIfEmpty(region),
            PostalCode = NullIfEmpty(postalCode),
            CountryCode = string.IsNullOrWhiteSpace(countryCode) ? "IN" : countryCode.Trim().ToUpperInvariant()
        };
    }

    public void Update(
        string name,
        string? addressLine,
        string? city,
        string? region,
        string? postalCode,
        string? countryCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        AddressLine = NullIfEmpty(addressLine);
        City = NullIfEmpty(city);
        Region = NullIfEmpty(region);
        PostalCode = NullIfEmpty(postalCode);
        CountryCode = string.IsNullOrWhiteSpace(countryCode) ? "IN" : countryCode.Trim().ToUpperInvariant();
        Touch();
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
