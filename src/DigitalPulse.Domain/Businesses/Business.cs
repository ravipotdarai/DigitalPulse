using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Businesses;

public sealed class Business : TenantOwnedEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Website { get; private set; }

    private readonly List<BusinessLocation> _locations = [];
    public IReadOnlyCollection<BusinessLocation> Locations => _locations;

    private Business() { }

    public static Business Create(Guid tenantId, string name, string? website)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Business
        {
            TenantId = tenantId,
            Name = name.Trim(),
            Website = string.IsNullOrWhiteSpace(website) ? null : website.Trim()
        };
    }

    public void Update(string name, string? website)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Website = string.IsNullOrWhiteSpace(website) ? null : website.Trim();
        Touch();
    }

    public BusinessLocation AddLocation(string name, string? addressLine, string? city, string? region, string? postalCode, string? countryCode)
    {
        var location = BusinessLocation.Create(TenantId, Id, name, addressLine, city, region, postalCode, countryCode);
        _locations.Add(location);
        Touch();
        return location;
    }
}
