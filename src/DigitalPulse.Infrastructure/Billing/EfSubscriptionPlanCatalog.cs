using DigitalPulse.Application.Abstractions;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Tenancy;
using DigitalPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Billing;

public sealed class EfSubscriptionPlanCatalog : ISubscriptionPlanCatalog
{
    private readonly AppDbContext _db;

    public EfSubscriptionPlanCatalog(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SubscriptionPlan>> ListAsync(CancellationToken cancellationToken) =>
        await _db.Plans.AsNoTracking().OrderBy(p => p.SortOrder).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SubscriptionPlan>> ListForTenantTypeAsync(
        TenantType tenantType,
        CancellationToken cancellationToken)
    {
        var all = await ListAsync(cancellationToken);
        return all.Where(p => p.IsAvailableTo(tenantType)).ToList();
    }

    public Task<SubscriptionPlan?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Code == code.Trim().ToUpperInvariant(), cancellationToken);
}
