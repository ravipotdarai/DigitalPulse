using DigitalPulse.Application.Features.Content;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class ContentHubPhase9Tests
{
    [Fact]
    public async Task Variants_stay_on_the_same_article_and_are_structurally_different()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var created = await new CreateHubContentHandler(db, tenant, user, search).Handle(
            business.Id,
            new CreateHubContentRequest("ARTICLE", "How to choose a conference room", "how-to-choose-a-conference-room", "A practical excerpt for the hub checklist snippet.", "# How\n\nEnough body for the validator and a distinct website article.", "Public", null, null, null, null, null),
            CancellationToken.None);
        var before = await db.ContentItems.CountAsync();
        var variants = await new CreateHubVariantsHandler(db, tenant).Handle(business.Id, created.Id, CancellationToken.None);
        Assert.Equal(created.Id, variants.Id);
        Assert.Equal(before, await db.ContentItems.CountAsync());
        Assert.True(variants.Variants.Count >= 8);
        Assert.Equal(variants.Variants.Count, variants.Variants.Select(item => item.Body).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(variants.Variants, item => item.Kind == "YouTubeMetadata" && item.Body.Contains("YouTube script"));
        Assert.Contains(variants.Variants, item => item.Kind == "WebsiteArticle" && item.Body.Contains("Enough body"));
        var again = await new CreateHubVariantsHandler(db, tenant).Handle(business.Id, created.Id, CancellationToken.None);
        Assert.Equal(variants.Variants.Count, again.Variants.Count);
    }

    private static async Task<(AppDbContext Db, FixedTenantContext Tenant, FixedUser User, InMemorySearchProvider Search, Business Business)> SeedAsync()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "A Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"hub-p9-{Guid.NewGuid()}")
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
