using DigitalPulse.Application.Abstractions;
using DigitalPulse.Contracts.Businesses;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Businesses;

public sealed class ListBusinessesHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ListBusinessesHandler(IAppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<BusinessResponse>> Handle(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        return await _db.Businesses.AsNoTracking()
            .Where(b => b.TenantId == tenantId)
            .OrderBy(b => b.Name)
            .Select(b => new BusinessResponse(b.Id, b.TenantId, b.Name, b.Website))
            .ToListAsync(cancellationToken);
    }
}
