namespace DigitalPulse.Contracts.Auth;

public sealed record RegisterRequest(string Email, string DisplayName, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record UpdateProfileRequest(string DisplayName);

public sealed record AuthResponse(
    string AccessToken,
    Guid UserId,
    string Email,
    string DisplayName,
    Guid? TenantId,
    string? TenantName,
    string? TenantType);

public sealed record MeResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    Guid? TenantId,
    string? TenantName,
    string? TenantType);
