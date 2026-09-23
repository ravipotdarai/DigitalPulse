namespace DigitalPulse.Contracts.Businesses;

public sealed record CreateBusinessRequest(string Name, string? Website);

public sealed record UpdateBusinessRequest(string Name, string? Website);

public sealed record BusinessResponse(Guid Id, Guid TenantId, string Name, string? Website);

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
