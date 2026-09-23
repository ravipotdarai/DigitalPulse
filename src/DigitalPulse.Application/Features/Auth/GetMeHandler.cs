using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Contracts.Auth;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Auth;

public sealed class GetMeHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMeHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<MeResponse> Handle(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw AppException.Unauthorized();
        }

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken)
            ?? throw AppException.Unauthorized();

        var membership = await _db.Memberships.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == user.Id, cancellationToken);
        var tenant = membership is null
            ? null
            : await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == membership.TenantId, cancellationToken);

        return new MeResponse(user.Id, user.Email, user.DisplayName, tenant?.Id, tenant?.Name, tenant?.Type.ToString());
    }
}
