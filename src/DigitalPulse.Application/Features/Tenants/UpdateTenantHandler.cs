using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Contracts.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Tenants;

public sealed class UpdateTenantHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public UpdateTenantHandler(IAppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<TenantResponse> Handle(UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw AppException.NotFound("Tenant was not found.");
        tenant.Rename(request.Name);
        await _db.SaveChangesAsync(cancellationToken);
        return new TenantResponse(tenant.Id, tenant.Name, tenant.Type.ToString());
    }
}
