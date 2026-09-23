using DigitalPulse.Application.Abstractions;
using DigitalPulse.Contracts.Auth;
using DigitalPulse.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Auth;

public sealed class RegisterUserHandler
{
    private readonly IAppDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly IAuthTokenIssuer _tokens;

    public RegisterUserHandler(IAppDbContext db, IPasswordService passwords, IAuthTokenIssuer tokens)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
    }

    public async Task<AuthResponse> Handle(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw Common.AppException.Conflict("An account with this email already exists.");
        }

        var user = AppUser.Register(email, request.DisplayName, _passwords.Hash(request.Password));
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(_tokens.Issue(user, null), user.Id, user.Email, user.DisplayName, null, null, null);
    }
}
