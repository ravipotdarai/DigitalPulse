using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Contracts.Businesses;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Businesses;

public sealed class UpdateBusinessHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public UpdateBusinessHandler(IAppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<BusinessResponse> Handle(Guid businessId, UpdateBusinessRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var business = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == businessId && b.TenantId == tenantId, cancellationToken)
            ?? throw AppException.NotFound("Business was not found.");
        business.Update(request.Name, request.Website);
        await _db.SaveChangesAsync(cancellationToken);
        return new BusinessResponse(business.Id, business.TenantId, business.Name, business.Website, business.FoundedYear, business.BrandVoice, business.IndustryCode);
    }
}
