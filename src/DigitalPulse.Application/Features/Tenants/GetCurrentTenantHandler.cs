using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Contracts.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Tenants;

public sealed class GetCurrentTenantHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetCurrentTenantHandler(IAppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<TenantResponse> Handle(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw AppException.NotFound("Tenant was not found.");

        return new TenantResponse(tenant.Id, tenant.Name, tenant.Type.ToString());
    }
}
