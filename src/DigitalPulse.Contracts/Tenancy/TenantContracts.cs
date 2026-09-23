namespace DigitalPulse.Contracts.Tenancy;

public sealed record CreateTenantRequest(string Name, string Type);

public sealed record UpdateTenantRequest(string Name);

public sealed record TenantResponse(Guid Id, string Name, string Type);
