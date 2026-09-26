using DigitalPulse.Application.Features.Content;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class ContentHubPhase7Tests
{
    [Fact]
    public async Task Discover_offers_service_ideas_and_manual_can_be_dismissed()
    {
        var (db, tenant, business) = await SeedAsync();
        db.Services.Add(Service.Create(tenant.RequireTenantId(), business.Id, "Conference rooms", "Stored AV rooms."));
        await db.SaveChangesAsync();

        var workspace = await new DiscoverContentOpportunitiesHandler(db, tenant).Handle(business.Id, CancellationToken.None);
        Assert.Contains(workspace.Opportunities, item => item.SourceType == "Service" && item.Topic.Contains("Conference rooms"));
        Assert.All(workspace.Opportunities, item => Assert.True((item.CoverageScore ?? 0) + (item.OpportunityScore ?? 0) == 100));

        var created = await new CreateContentOpportunityHandler(db, tenant).Handle(
            business.Id,
            new CreateContentOpportunityRequest("Hybrid meeting rooms", "Manual idea from the desk."),
            CancellationToken.None);
        var manual = Assert.Single(created.Opportunities, item => item.SourceType == "Manual");
        Assert.Equal("Hybrid meeting rooms", manual.Topic);

        var dismissed = await new DismissContentOpportunityHandler(db, tenant).Handle(business.Id, manual.Id, CancellationToken.None);
        Assert.DoesNotContain(dismissed.Opportunities, item => item.Id == manual.Id);
    }

    private static async Task<(AppDbContext Db, FixedTenantContext Tenant, Business Business)> SeedAsync()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "A Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"hub-p7-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options, new FixedTenantContext(tenantId));
        db.Businesses.Add(business);
        await db.SaveChangesAsync();
        return (db, new FixedTenantContext(tenantId), business);
    }

    private sealed class FixedTenantContext : Application.Abstractions.ITenantContext
    {
        public FixedTenantContext(Guid tenantId) => TenantId = tenantId;
        public Guid? TenantId { get; }
        public Guid RequireTenantId() => TenantId!.Value;
    }
}
