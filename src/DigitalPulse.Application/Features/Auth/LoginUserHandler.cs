using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Contracts.Auth;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Auth;

public sealed class LoginUserHandler
{
    private readonly IAppDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly IAuthTokenIssuer _tokens;

    public LoginUserHandler(IAppDbContext db, IPasswordService passwords, IAuthTokenIssuer tokens)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
    }

    public async Task<AuthResponse> Handle(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null || !_passwords.Verify(user.PasswordHash, request.Password))
        {
            throw AppException.Unauthorized("Invalid email or password.");
        }

        var membership = await _db.Memberships.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == user.Id, cancellationToken);
        var tenant = membership is null
            ? null
            : await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == membership.TenantId, cancellationToken);

        return new AuthResponse(
            _tokens.Issue(user, tenant),
            user.Id,
            user.Email,
            user.DisplayName,
            tenant?.Id,
            tenant?.Name,
            tenant?.Type.ToString());
    }
}
