using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Contracts.Auth;
using DigitalPulse.Contracts.Tenancy;
using DigitalPulse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Tenants;

public sealed class CreateTenantHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuthTokenIssuer _tokens;

    public CreateTenantHandler(IAppDbContext db, ICurrentUser currentUser, IAuthTokenIssuer tokens)
    {
        _db = db;
        _currentUser = currentUser;
        _tokens = tokens;
    }

    public async Task<AuthResponse> Handle(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw AppException.Unauthorized();
        }

        if (await _db.Memberships.IgnoreQueryFilters().AnyAsync(m => m.UserId == _currentUser.UserId, cancellationToken))
        {
            throw AppException.Conflict("You already belong to a tenant.");
        }

        if (!Enum.TryParse<TenantType>(request.Type, ignoreCase: true, out var type) || type == TenantType.Platform)
        {
            throw AppException.Validation("Tenant type must be Direct or Agency.");
        }

        var user = await _db.Users.FirstAsync(u => u.Id == _currentUser.UserId, cancellationToken);
        var tenant = Tenant.Create(request.Name, type);
        _db.Tenants.Add(tenant);
        _db.Memberships.Add(TenantMembership.CreateOwner(tenant.Id, user.Id));
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            _tokens.Issue(user, tenant),
            user.Id,
            user.Email,
            user.DisplayName,
            tenant.Id,
            tenant.Name,
            tenant.Type.ToString());
    }
}
