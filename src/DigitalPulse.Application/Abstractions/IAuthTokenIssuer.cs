using DigitalPulse.Domain.Identity;
using DigitalPulse.Domain.Tenancy;

namespace DigitalPulse.Application.Abstractions;

public sealed record AuthTokenClaims(Guid UserId, string Email, Guid? TenantId, string? TenantType);

public interface IAuthTokenIssuer
{
    string Issue(AppUser user, Tenant? tenant);
    AuthTokenClaims Read(string token);
}
