using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Tenancy;
using DigitalPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class TenantIsolationTests
{
    [Fact]
    public async Task Query_filter_hides_other_tenant_businesses()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"iso-{Guid.NewGuid()}")
            .Options;

        await using (var seed = new AppDbContext(options, tenantContext: null))
        {
            seed.Businesses.Add(Business.Create(tenantA, "A Co", null));
            seed.Businesses.Add(Business.Create(tenantB, "B Co", null));
            await seed.SaveChangesAsync();
        }

        var contextA = new FixedTenantContext(tenantA);
        await using var dbA = new AppDbContext(options, contextA);
        var visible = await dbA.Businesses.Select(b => b.Name).ToListAsync();
        Assert.Equal(["A Co"], visible);
    }

    private sealed class FixedTenantContext : Application.Abstractions.ITenantContext
    {
        public FixedTenantContext(Guid tenantId) => TenantId = tenantId;
        public Guid? TenantId { get; }
        public Guid RequireTenantId() => TenantId!.Value;
    }
}
