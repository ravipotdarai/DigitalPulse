using DigitalPulse.Application.Features.Content;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class ContentHubPhase3Tests
{
    [Fact]
    public async Task Register_media_rejects_private_and_http_hosts()
    {
        var (db, tenant, business) = await SeedAsync();
        var handler = new RegisterHubMediaHandler(db, tenant);

        var loopback = await Assert.ThrowsAsync<Application.Common.AppException>(() =>
            handler.Handle(business.Id, new RegisterHubMediaRequest("Hero", "Image", "http://127.0.0.1/hero.jpg"), CancellationToken.None));
        Assert.Equal(400, loopback.StatusCode);

        var http = await Assert.ThrowsAsync<Application.Common.AppException>(() =>
            handler.Handle(business.Id, new RegisterHubMediaRequest("Hero", "Image", "http://example.com/hero.jpg"), CancellationToken.None));
        Assert.Equal(400, http.StatusCode);
        Assert.Contains("https", http.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Featured_image_url_is_returned_on_the_public_article()
    {
        var (db, tenant, business) = await SeedAsync();
        var user = new FixedUser();
        var search = new InMemorySearchProvider();
        var asset = await new RegisterHubMediaHandler(db, tenant).Handle(
            business.Id,
            new RegisterHubMediaRequest("Hero", "Image", "https://example.com/hero.jpg"),
            CancellationToken.None);

        var workspace = await new GetContentHubHandler(db, tenant).Handle(business.Id, CancellationToken.None);
        Assert.Contains(workspace.Media, item => item.Id == asset.Id && item.SourceUrl == "https://example.com/hero.jpg");

        var created = await new CreateHubContentHandler(db, tenant, user, search).Handle(
            business.Id,
            DraftRequest() with { FeaturedMediaAssetId = asset.Id, Slug = "public-guide" },
            CancellationToken.None);
        Assert.Equal(asset.Id, created.FeaturedMediaAssetId);

        await new ApproveHubContentHandler(db, tenant).Handle(business.Id, created.Id, CancellationToken.None);
        await new PublishHubContentHandler(db, tenant, search).Handle(business.Id, created.Id, CancellationToken.None);

        var article = await new GetPublicHubArticleHandler(db).Handle(business.Id, "public-guide", CancellationToken.None);
        Assert.Equal("https://example.com/hero.jpg", article.FeaturedImageUrl);

        var index = await new GetPublicHubIndexHandler(db).Handle(business.Id, CancellationToken.None);
        Assert.Equal("https://example.com/hero.jpg", index.Articles[0].FeaturedImageUrl);
    }

    private static CreateHubContentRequest DraftRequest() =>
        new("ARTICLE", "How to choose", "how-to-choose", "A practical excerpt for the unique slug test.", "# How\n\nEnough body for the validator.", "Public", null, null, null, null, null);

    private static async Task<(AppDbContext Db, FixedTenantContext Tenant, Business Business)> SeedAsync()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "A Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"hub-p3-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options, new FixedTenantContext(tenantId));
        db.Businesses.Add(business);
        await db.SaveChangesAsync();
        return (db, new FixedTenantContext(tenantId), business);
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
