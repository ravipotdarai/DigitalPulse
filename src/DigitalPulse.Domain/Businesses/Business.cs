using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Businesses;

public sealed class Business : TenantOwnedEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Website { get; private set; }
    public int? FoundedYear { get; private set; }
    public string? BrandVoice { get; private set; }
    public string? IndustryCode { get; private set; }

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

    public void Update(string name, string? website) =>
        UpdateIdentity(name, website, FoundedYear, BrandVoice, IndustryCode);

    public void UpdateIdentity(string name, string? website, int? foundedYear, string? brandVoice, string? industryCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (foundedYear is < 1800 or > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(foundedYear), "Founded year is out of range.");
        }

        Name = name.Trim();
        Website = string.IsNullOrWhiteSpace(website) ? null : website.Trim();
        FoundedYear = foundedYear;
        BrandVoice = string.IsNullOrWhiteSpace(brandVoice) ? null : brandVoice.Trim();
        IndustryCode = string.IsNullOrWhiteSpace(industryCode) ? null : industryCode.Trim().ToUpperInvariant();
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
