using DigitalPulse.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace DigitalPulse.Infrastructure.Auth;

public sealed class AspNetPasswordService : IPasswordService
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(this, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(this, hash, password) != PasswordVerificationResult.Failed;
}
