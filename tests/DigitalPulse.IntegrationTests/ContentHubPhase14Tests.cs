using DigitalPulse.Application.Features.Content;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Domain.Agency;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class ContentHubPhase14Tests
{
    [Fact]
    public async Task Public_hub_uses_enabled_white_label_without_crossing_businesses()
    {
        var tenantId = Guid.NewGuid();
        var brand = WhiteLabelProfile.Create(tenantId);
        brand.Apply("Studio North", "hello@studio.test", null, "#112233", "https://example.com/logo.png", null, true);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"hub-p14-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options, new FixedTenantContext(tenantId));
        var a = Business.Create(tenantId, "A Co", null);
        var b = Business.Create(tenantId, "B Co", null);
        db.Businesses.AddRange(a, b);
        db.WhiteLabelProfiles.Add(brand);
        await db.SaveChangesAsync();

        var tenant = new FixedTenantContext(tenantId);
        var user = new FixedUser();
        var search = new InMemorySearchProvider();
        var created = await new CreateHubContentHandler(db, tenant, user, search).Handle(
            a.Id,
            new CreateHubContentRequest("ARTICLE", "How to choose", "how-to-choose", "A practical excerpt for the unique slug test.", "# How\n\nEnough body for the validator.", "Public", null, null, null, null, null),
            CancellationToken.None);
        await new SubmitHubApprovalHandler(db, tenant).Handle(a.Id, created.Id, CancellationToken.None);
        await new ApproveHubContentHandler(db, tenant).Handle(a.Id, created.Id, CancellationToken.None);
        await new PublishHubContentHandler(db, tenant, search).Handle(a.Id, created.Id, CancellationToken.None);

        var index = await new GetPublicHubIndexHandler(db).Handle(a.Id, CancellationToken.None);
        Assert.True(index.WhiteLabel);
        Assert.Equal("Studio North", index.BrandName);
        Assert.Equal("#112233", index.PrimaryColor);

        var other = await new GetContentHubHandler(db, tenant).Handle(b.Id, CancellationToken.None);
        Assert.DoesNotContain(other.Items, item => item.Id == created.Id);
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
