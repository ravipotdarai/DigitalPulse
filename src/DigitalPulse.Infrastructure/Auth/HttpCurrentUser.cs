using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DigitalPulse.Infrastructure.Auth;

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public HttpCurrentUser(IHttpContextAccessor http) => _http = http;

    public bool IsAuthenticated =>
        _http.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid UserId =>
        Guid.TryParse(_http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw AppException.Unauthorized();

    public string Email =>
        _http.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
}
