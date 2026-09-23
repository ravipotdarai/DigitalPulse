namespace DigitalPulse.Contracts.Businesses;

public sealed record CreateBusinessRequest(string Name, string? Website);

public sealed record UpdateBusinessRequest(string Name, string? Website);

public sealed record UpdateBusinessProfileRequest(
    string Name,
    string? Website,
    int? FoundedYear,
    string? BrandVoice,
    string? IndustryCode);

public sealed record BusinessResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Website,
    int? FoundedYear = null,
    string? BrandVoice = null,
    string? IndustryCode = null);

public sealed record CreateLocationRequest(
    string Name,
    string? AddressLine,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode);

public sealed record LocationResponse(
    Guid Id,
    Guid BusinessId,
    Guid TenantId,
    string Name,
    string? AddressLine,
    string? City,
    string? Region,
    string? PostalCode,
    string CountryCode);

public sealed record CatalogItemResponse(string Code, string Name);

public sealed record ContactPointRequest(string Kind, string Value, string? Label);

public sealed record ContactPointResponse(Guid Id, Guid BusinessId, string Kind, string Value, string? Label);

public sealed record NamedItemRequest(string Name, string? Description = null);

public sealed record NamedItemResponse(Guid Id, string Name, string? Description = null);

public sealed record BusinessBrandResponse(Guid Id, Guid BrandId, string Name);

public sealed record FactRequest(string FactTypeCode, string Value, string Status);

public sealed record FactResponse(Guid Id, string FactTypeCode, string Value, string Status, bool CanPublish);

public sealed record CustomerRequest(string DisplayName, string Mobile, string? Email, string? Notes);

public sealed record CustomerContactRequest(string Kind, string Value);

public sealed record CustomerContactResponse(Guid Id, string Kind, string Value);

public sealed record CustomerResponse(
    Guid Id,
    string DisplayName,
    string? Notes,
    IReadOnlyList<CustomerContactResponse> Contacts);

public sealed record BusinessIdentityResponse(
    BusinessResponse Business,
    IReadOnlyList<CatalogItemResponse> Industries,
    IReadOnlyList<CatalogItemResponse> FactTypes,
    IReadOnlyList<ContactPointResponse> Contacts,
    IReadOnlyList<NamedItemResponse> Categories,
    IReadOnlyList<NamedItemResponse> Services,
    IReadOnlyList<BusinessBrandResponse> Brands,
    IReadOnlyList<FactResponse> Facts,
    IReadOnlyList<CustomerResponse> Customers);
