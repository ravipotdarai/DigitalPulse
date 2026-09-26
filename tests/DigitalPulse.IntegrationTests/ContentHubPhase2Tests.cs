using DigitalPulse.Application.Features.Content;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Content;
using DigitalPulse.Domain.Projects;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class ContentHubPhase2Tests
{
    [Fact]
    public async Task Publish_without_approval_is_rejected()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var create = new CreateHubContentHandler(db, tenant, user, search);
        var created = await create.Handle(business.Id, DraftRequest(), CancellationToken.None);
        var publish = new PublishHubContentHandler(db, tenant, search);
        var error = await Assert.ThrowsAsync<Application.Common.AppException>(() => publish.Handle(business.Id, created.Id, CancellationToken.None));
        Assert.Equal(400, error.StatusCode);
        Assert.Contains("Approve", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Public_index_lists_only_published_public_articles()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var create = new CreateHubContentHandler(db, tenant, user, search);
        var draft = await create.Handle(business.Id, DraftRequest() with { Visibility = "Private", Slug = "private-guide" }, CancellationToken.None);
        var live = await create.Handle(business.Id, DraftRequest() with { Title = "How to publish a public guide", Slug = "public-guide", Visibility = "Public" }, CancellationToken.None);
        var submit = new SubmitHubApprovalHandler(db, tenant);
        var approve = new ApproveHubContentHandler(db, tenant);
        await submit.Handle(business.Id, live.Id, CancellationToken.None);
        await approve.Handle(business.Id, live.Id, CancellationToken.None);
        await new PublishHubContentHandler(db, tenant, search).Handle(business.Id, live.Id, CancellationToken.None);
        await submit.Handle(business.Id, draft.Id, CancellationToken.None);
        await approve.Handle(business.Id, draft.Id, CancellationToken.None);
        await new PublishHubContentHandler(db, tenant, search).Handle(business.Id, draft.Id, CancellationToken.None);

        var index = await new GetPublicHubIndexHandler(db).Handle(business.Id, CancellationToken.None);
        Assert.Equal(business.Name, index.BusinessName);
        Assert.Single(index.Articles);
        Assert.Equal("public-guide", index.Articles[0].Slug);
    }

    [Fact]
    public async Task Restore_revision_writes_a_new_draft_version()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var create = new CreateHubContentHandler(db, tenant, user, search);
        var created = await create.Handle(business.Id, DraftRequest(), CancellationToken.None);
        var first = created.Revisions[0];
        await new UpdateHubContentHandler(db, tenant, user, search).Handle(
            business.Id,
            created.Id,
            new UpdateHubContentRequest("ARTICLE", "How to choose after edit", created.Slug, created.Excerpt, "# Edited\n\nEnough body for the validator after restore.", "Public", null, null, null, "Edited", null, null),
            CancellationToken.None);
        var restored = await new RestoreHubRevisionHandler(db, tenant, user, search).Handle(business.Id, created.Id, first.Id, CancellationToken.None);
        Assert.Equal("How to choose", restored.Title);
        Assert.Equal("Draft", restored.Status);
        Assert.Contains(restored.Revisions, r => r.ChangeSummary.Contains("Restored", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Media_attach_requires_same_business_asset()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var other = Business.Create(tenant.RequireTenantId(), "Other Co", null);
        db.Businesses.Add(other);
        var asset = MediaAsset.Register(tenant.RequireTenantId(), other.Id, "Hero", MediaKind.Image, "https://example.com/hero.jpg");
        db.MediaAssets.Add(asset);
        await db.SaveChangesAsync();
        var created = await new CreateHubContentHandler(db, tenant, user, search).Handle(business.Id, DraftRequest(), CancellationToken.None);
        var error = await Assert.ThrowsAsync<Application.Common.AppException>(() =>
            new AttachHubMediaHandler(db, tenant).Handle(business.Id, created.Id, new AttachHubMediaRequest(asset.Id, "Featured"), CancellationToken.None));
        Assert.Equal(404, error.StatusCode);

        var local = MediaAsset.Register(tenant.RequireTenantId(), business.Id, "Hero", MediaKind.Image, "https://example.com/local.jpg");
        db.MediaAssets.Add(local);
        await db.SaveChangesAsync();
        var attached = await new AttachHubMediaHandler(db, tenant).Handle(business.Id, created.Id, new AttachHubMediaRequest(local.Id, "Featured"), CancellationToken.None);
        Assert.Equal(local.Id, attached.FeaturedMediaAssetId);
        Assert.Contains(attached.Media, m => m.Role == "Featured" && m.MediaAssetId == local.Id);
    }

    private static CreateHubContentRequest DraftRequest() =>
        new("ARTICLE", "How to choose", "how-to-choose", "A practical excerpt for the unique slug test.", "# How\n\nEnough body for the validator.", "Public", null, null, null, null, null);

    private static async Task<(AppDbContext Db, FixedTenantContext Tenant, FixedUser User, InMemorySearchProvider Search, Business Business)> SeedAsync()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "A Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"hub-p2-{Guid.NewGuid()}")
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
