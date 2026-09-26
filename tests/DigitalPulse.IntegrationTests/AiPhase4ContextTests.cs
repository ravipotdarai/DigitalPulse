using DigitalPulse.Application.Ai;
using DigitalPulse.Domain.Ai;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class AiPhase4ContextTests
{
    [Fact]
    public async Task Context_builder_stays_inside_the_session_tenant_and_business()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var businessA = Business.Create(tenantA, "AV Professionals", null);
        var businessB = Business.Create(tenantB, "Other Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ai-p4-{Guid.NewGuid()}")
            .Options;
        await using var seed = new AppDbContext(options, tenantContext: null);
        seed.Businesses.AddRange(businessA, businessB);
        seed.Facts.Add(BusinessFact.Create(tenantA, businessA.Id, "NAME", "AV Professionals", FactStatus.Approved));
        seed.Facts.Add(BusinessFact.Create(tenantB, businessB.Id, "NAME", "Other Co", FactStatus.Approved));
        seed.GraphNodes.Add(GraphNode.Create(tenantA, businessA.Id, GraphNodeKind.Business, "AV Professionals", $"business:{businessA.Id}", "Conference rooms"));
        seed.GraphNodes.Add(GraphNode.Create(tenantB, businessB.Id, GraphNodeKind.Business, "Other Co", $"business:{businessB.Id}", "Should not leak"));
        await seed.SaveChangesAsync();

        await using var db = new AppDbContext(options, new FixedTenant(tenantA));
        var builder = new AiContextBuilder(db, new InMemorySearchProvider());
        var built = await builder.BuildAsync(tenantA, businessA.Id, "conference room", CancellationToken.None);

        Assert.Contains(built.Evidence, item => item.Body.Contains("AV Professionals"));
        Assert.DoesNotContain(built.Evidence, item => item.Body.Contains("Other Co") || item.Body.Contains("Should not leak"));
        Assert.Contains(built.GraphLines, line => line.Contains("AV Professionals"));
        Assert.DoesNotContain(built.GraphLines, line => line.Contains("Should not leak"));
    }

    private sealed class FixedTenant : Application.Abstractions.ITenantContext
    {
        public FixedTenant(Guid tenantId) => TenantId = tenantId;
        public Guid? TenantId { get; }
        public Guid RequireTenantId() => TenantId!.Value;
    }
}
