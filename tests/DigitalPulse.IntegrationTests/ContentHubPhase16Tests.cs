using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Content;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Projects;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class ContentHubPhase16Tests
{
    [Fact]
    public async Task Public_hub_hides_drafts_and_private_published()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        await new CreateHubContentHandler(db, tenant, user, search).Handle(
            business.Id,
            new CreateHubContentRequest("ARTICLE", "Draft stay", "draft-stay", "A practical excerpt for the unique slug test.", "# Draft\n\nEnough body for the validator.", "Public", null, null, null, null, null),
            CancellationToken.None);

        var privateItem = await new CreateHubContentHandler(db, tenant, user, search).Handle(
            business.Id,
            new CreateHubContentRequest("ARTICLE", "Private stay", "private-stay", "A practical excerpt for the unique slug test.", "# Private\n\nEnough body for the validator.", "Private", null, null, null, null, null),
            CancellationToken.None);
        await new SubmitHubApprovalHandler(db, tenant).Handle(business.Id, privateItem.Id, CancellationToken.None);
        await new ApproveHubContentHandler(db, tenant).Handle(business.Id, privateItem.Id, CancellationToken.None);
        await new PublishHubContentHandler(db, tenant, search).Handle(business.Id, privateItem.Id, CancellationToken.None);

        var index = await new GetPublicHubIndexHandler(db).Handle(business.Id, CancellationToken.None);
        Assert.Empty(index.Articles);
        var missing = await Assert.ThrowsAsync<AppException>(() =>
            new GetPublicHubArticleHandler(db).Handle(business.Id, "draft-stay", CancellationToken.None));
        Assert.Equal(404, missing.StatusCode);
    }

    [Fact]
    public async Task Workspace_cannot_read_another_business_article()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var other = Business.Create(tenant.RequireTenantId(), "B Co", null);
        db.Businesses.Add(other);
        await db.SaveChangesAsync();
        var created = await new CreateHubContentHandler(db, tenant, user, search).Handle(
            business.Id,
            new CreateHubContentRequest("ARTICLE", "How to choose", "how-to-choose", "A practical excerpt for the unique slug test.", "# How\n\nEnough body for the validator.", "Public", null, null, null, null, null),
            CancellationToken.None);

        var hidden = await Assert.ThrowsAsync<AppException>(() =>
            new GetHubContentHandler(db, tenant).Handle(other.Id, created.Id, CancellationToken.None));
        Assert.Equal(404, hidden.StatusCode);

        var workspace = await new GetContentHubHandler(db, tenant).Handle(other.Id, CancellationToken.None);
        Assert.DoesNotContain(workspace.Items, item => item.Id == created.Id);
    }

    [Fact]
    public async Task Public_media_is_held_until_the_article_is_public()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var asset = MediaAsset.Register(tenant.RequireTenantId(), business.Id, "Hero", MediaKind.Image, "hero.jpg");
        db.MediaAssets.Add(asset);
        await db.SaveChangesAsync();
        await new CreateHubContentHandler(db, tenant, user, search).Handle(
            business.Id,
            new CreateHubContentRequest("ARTICLE", "How to choose", "how-to-choose", "A practical excerpt for the unique slug test.", "# How\n\nEnough body for the validator.", "Public", null, null, null, null, null, asset.Id),
            CancellationToken.None);

        var missing = await Assert.ThrowsAsync<AppException>(() =>
            new GetPublicHubMediaHandler(db, new RejectingStore()).Handle(business.Id, asset.Id, CancellationToken.None));
        Assert.Equal(404, missing.StatusCode);
    }

    [Fact]
    public async Task Publish_writes_an_operations_audit_without_secrets()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var created = await new CreateHubContentHandler(db, tenant, user, search).Handle(
            business.Id,
            new CreateHubContentRequest("ARTICLE", "How to choose", "how-to-choose", "A practical excerpt for the unique slug test.", "# How\n\nEnough body for the validator.", "Public", null, null, null, null, null),
            CancellationToken.None);
        await new SubmitHubApprovalHandler(db, tenant).Handle(business.Id, created.Id, CancellationToken.None);
        await new ApproveHubContentHandler(db, tenant).Handle(business.Id, created.Id, CancellationToken.None);
        await new PublishHubContentHandler(db, tenant, search).Handle(business.Id, created.Id, CancellationToken.None);

        var actions = await db.OperationsAudits.Select(a => a.Action).ToListAsync();
        Assert.Contains("content.submit", actions);
        Assert.Contains("content.approve", actions);
        Assert.Contains("content.publish", actions);
        Assert.All(actions, action => Assert.DoesNotContain("token", action, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Canonical_url_rejects_private_hosts()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var clash = await Assert.ThrowsAsync<AppException>(() =>
            new CreateHubContentHandler(db, tenant, user, search).Handle(
                business.Id,
                new CreateHubContentRequest("ARTICLE", "How to choose", "how-to-choose", "A practical excerpt for the unique slug test.", "# How\n\nEnough body for the validator.", "Public", null, null, "http://127.0.0.1/guide", null, null),
                CancellationToken.None));
        Assert.Equal(400, clash.StatusCode);
    }

    private static async Task<(AppDbContext Db, FixedTenantContext Tenant, FixedUser User, InMemorySearchProvider Search, Business Business)> SeedAsync()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "A Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"hub-p16-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options, new FixedTenantContext(tenantId));
        db.Businesses.Add(business);
        await db.SaveChangesAsync();
        return (db, new FixedTenantContext(tenantId), new FixedUser(), new InMemorySearchProvider(), business);
    }

    private sealed class RejectingStore : Application.Abstractions.ISocialMediaStore
    {
        public Task<Application.Abstractions.StoredSocialMedia> SaveAsync(Guid tenantId, Guid id, string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Public media must not read unpublished files.");

        public Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Public media must not read unpublished files.");
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
