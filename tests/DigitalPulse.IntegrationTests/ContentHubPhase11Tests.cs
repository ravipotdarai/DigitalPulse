using DigitalPulse.Application.Features.Content;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class ContentHubPhase11Tests
{
    [Fact]
    public async Task Unsupported_providers_stay_assisted_or_manual()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var workspace = await new GetContentHubHandler(db, tenant).Handle(business.Id, CancellationToken.None);
        Assert.Contains(workspace.Channels, item => item.ProviderCode == "HUB" && item.Mode == "Supported");
        Assert.Contains(workspace.Channels, item => item.ProviderCode == "INDIAMART" && item.Mode == "Manual");
        Assert.Contains(workspace.Channels, item => item.ProviderCode == "LINKEDIN" && item.Mode == "Assisted");

        var created = await new CreateHubContentHandler(db, tenant, user, search).Handle(
            business.Id,
            new CreateHubContentRequest("ARTICLE", "How to choose", "how-to-choose", "A practical excerpt for the unique slug test.", "# How\n\nEnough body for the validator.", "Public", null, null, null, null, null),
            CancellationToken.None);
        await new SubmitHubApprovalHandler(db, tenant).Handle(business.Id, created.Id, CancellationToken.None);
        await new ApproveHubContentHandler(db, tenant).Handle(business.Id, created.Id, CancellationToken.None);
        var held = await new DistributeHubContentHandler(db, tenant).Handle(business.Id, created.Id, "INDIAMART", CancellationToken.None);
        Assert.Contains(held.Distributions, item => item.ProviderCode == "INDIAMART" && item.Status != "Published");
        Assert.Contains(held.Distributions, item => item.FailureReason != null && item.FailureReason.Contains("invent", StringComparison.OrdinalIgnoreCase));

        var live = await new DistributeHubContentHandler(db, tenant).Handle(business.Id, created.Id, "HUB", CancellationToken.None);
        Assert.Equal("Published", live.Status);
        Assert.Contains(live.Distributions, item => item.ProviderCode == "HUB" && item.Status == "Published");
    }

    private static async Task<(AppDbContext Db, FixedTenantContext Tenant, FixedUser User, InMemorySearchProvider Search, Business Business)> SeedAsync()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "A Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"hub-p11-{Guid.NewGuid()}")
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
