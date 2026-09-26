using DigitalPulse.Application.Features.Content;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Domain.Ai;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class ContentHubPhase15Tests
{
    [Fact]
    public async Task Publish_attaches_a_content_graph_node_for_search()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        db.GraphNodes.Add(GraphNode.Create(tenant.RequireTenantId(), business.Id, GraphNodeKind.Business, business.Name, $"business:{business.Id}", null));
        await db.SaveChangesAsync();
        var created = await new CreateHubContentHandler(db, tenant, user, search).Handle(
            business.Id,
            new CreateHubContentRequest("ARTICLE", "How to choose", "how-to-choose", "A practical excerpt for the unique slug test.", "# How\n\nEnough body for the validator.", "Public", null, null, null, null, null),
            CancellationToken.None);
        await new SubmitHubApprovalHandler(db, tenant).Handle(business.Id, created.Id, CancellationToken.None);
        await new ApproveHubContentHandler(db, tenant).Handle(business.Id, created.Id, CancellationToken.None);
        await new PublishHubContentHandler(db, tenant, search).Handle(business.Id, created.Id, CancellationToken.None);

        var node = await db.GraphNodes.FirstOrDefaultAsync(n => n.SourceKey == $"content:{created.Id}");
        Assert.NotNull(node);
        Assert.Contains("How to choose", node!.Label);
        Assert.Equal(GraphNodeKind.Content, node.Kind);
    }

    private static async Task<(AppDbContext Db, FixedTenantContext Tenant, FixedUser User, InMemorySearchProvider Search, Business Business)> SeedAsync()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "A Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"hub-p15-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options, new FixedTenantContext(tenantId));
        db.Businesses.Add(business);
        await db.SaveChangesAsync();
        return (db, new FixedTenantContext(tenantId), new FixedUser(), new InMemorySearchProvider(), business);
    }

    private sealed class FixedUser : Application.Abstractions.ICurrentUser
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public string Email => "tester@digitalpulse.test";
        public bool IsAuthenticated => true;
    }

    private sealed class FixedTenantContext : Application.Abstractions.ITenantContext
    {
        public FixedTenantContext(Guid tenantId) => TenantId = tenantId;
        public Guid? TenantId { get; }
        public Guid RequireTenantId() => TenantId!.Value;
    }
}
