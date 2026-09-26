using DigitalPulse.Domain.Ai;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class AiPhase10IsolationTests
{
    [Fact]
    public async Task Query_filter_hides_other_tenant_ai_runs()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var businessA = Business.Create(tenantA, "AV Professionals", null);
        var businessB = Business.Create(tenantB, "Other Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ai-p10-{Guid.NewGuid()}")
            .Options;

        await using (var seed = new AppDbContext(options, tenantContext: null))
        {
            seed.Businesses.AddRange(businessA, businessB);
            seed.AiRuns.Add(AiRun.Start(tenantA, businessA.Id, AiAgentKind.Social, "Generate a Google post"));
            seed.AiRuns.Add(AiRun.Start(tenantB, businessB.Id, AiAgentKind.Social, "Secret ask"));
            await seed.SaveChangesAsync();
        }

        await using var dbA = new AppDbContext(options, new FixedTenant(tenantA));
        var visible = await dbA.AiRuns.Select(r => r.Prompt).ToListAsync();
        Assert.Equal(["Generate a Google post"], visible);
    }

    private sealed class FixedTenant : Application.Abstractions.ITenantContext
    {
        public FixedTenant(Guid tenantId) => TenantId = tenantId;
        public Guid? TenantId { get; }
        public Guid RequireTenantId() => TenantId!.Value;
    }
}
