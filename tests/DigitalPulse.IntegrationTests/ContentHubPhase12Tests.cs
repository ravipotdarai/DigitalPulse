using DigitalPulse.Application.Features.Content;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class ContentHubPhase12Tests
{
    [Fact]
    public async Task Public_hub_includes_featured_services_and_stored_meta()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        db.Services.Add(Service.Create(tenant.RequireTenantId(), business.Id, "Conference rooms", null));
        await db.SaveChangesAsync();
        var created = await new CreateHubContentHandler(db, tenant, user, search).Handle(
            business.Id,
            new CreateHubContentRequest("ARTICLE", "How to publish a public guide", "public-guide", "A practical excerpt for the unique slug test.", "# How\n\nEnough body for the validator.", "Public", null, null, null, null, null, null, "How to publish a public guide", "A stored meta description that is long enough for a search snippet on this business."),
            CancellationToken.None);
        await new SubmitHubApprovalHandler(db, tenant).Handle(business.Id, created.Id, CancellationToken.None);
        await new ApproveHubContentHandler(db, tenant).Handle(business.Id, created.Id, CancellationToken.None);
        await new PublishHubContentHandler(db, tenant, search).Handle(business.Id, created.Id, CancellationToken.None);

        var index = await new GetPublicHubIndexHandler(db).Handle(business.Id, CancellationToken.None);
        Assert.Equal("public-guide", index.Featured?.Slug);
        Assert.Contains("Conference rooms", index.Services);
        Assert.Contains(business.Name, index.Cta);
        Assert.Contains(business.Name, index.MetaTitle);

        var article = await new GetPublicHubArticleHandler(db).Handle(business.Id, "public-guide", CancellationToken.None);
        Assert.Equal("How to publish a public guide", article.MetaTitle);
        Assert.StartsWith("A stored meta description", article.MetaDescription);
    }

    private static async Task<(AppDbContext Db, FixedTenantContext Tenant, FixedUser User, InMemorySearchProvider Search, Business Business)> SeedAsync()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "A Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"hub-p12-{Guid.NewGuid()}")
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
