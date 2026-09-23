using DigitalPulse.Application.Abstractions;
using DigitalPulse.Domain.Identity;
using DigitalPulse.Domain.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DigitalPulse.Infrastructure.Auth;

public sealed class DevelopmentJwtTokenIssuer : IAuthTokenIssuer
{
    public const string SchemeName = "DevelopmentJwt";

    private readonly byte[] _key;
    private readonly string _issuer;
    private readonly string _audience;

    public DevelopmentJwtTokenIssuer(IConfiguration configuration)
    {
        _issuer = configuration["Auth:Issuer"] ?? "digitalpulse";
        _audience = configuration["Auth:Audience"] ?? "digitalpulse-web";
        var secret = configuration["Auth:SigningKey"]
            ?? "dev-only-change-me-digitalpulse-signing-key-32";
        _key = Encoding.UTF8.GetBytes(secret);
    }

    public string Issue(AppUser user, Tenant? tenant)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("display_name", user.DisplayName)
        };

        if (tenant is not null)
        {
            claims.Add(new Claim("tenant_id", tenant.Id.ToString()));
            claims.Add(new Claim("tenant_type", tenant.Type.ToString()));
        }

        var token = new JwtSecurityToken(
            _issuer,
            _audience,
            claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public AuthTokenClaims Read(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, TokenValidationParameters(), out _);
        var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var tenantValue = principal.FindFirstValue("tenant_id");
        Guid? tenantId = Guid.TryParse(tenantValue, out var parsed) ? parsed : null;
        return new AuthTokenClaims(userId, email, tenantId, principal.FindFirstValue("tenant_type"));
    }

    public TokenValidationParameters TokenValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = _issuer,
        ValidAudience = _audience,
        IssuerSigningKey = new SymmetricSecurityKey(_key),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
}
