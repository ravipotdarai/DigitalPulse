using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Content;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class PublishingEnginePhaseCTests
{
    [Fact]
    public async Task Google_creates_one_row_per_owned_location()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var pune = BusinessLocation.Create(tenant.RequireTenantId(), business.Id, "Pune", null, "Pune", "MH", null, "IN");
        var mumbai = BusinessLocation.Create(tenant.RequireTenantId(), business.Id, "Mumbai", null, "Mumbai", "MH", null, "IN");
        db.Locations.AddRange(pune, mumbai);
        await db.SaveChangesAsync();
        var item = await ApproveArticleAsync(db, tenant, user, search, business.Id);

        var distributed = await new DistributeHubContentHandler(db, tenant).Handle(
            business.Id,
            item.Id,
            new DistributeHubContentRequest("GOOGLE"),
            CancellationToken.None);

        Assert.Equal(2, distributed.Distributions.Count(row => row.ProviderCode == "GOOGLE"));
        Assert.Contains(distributed.Distributions, row => row.LocationId == pune.Id);
        Assert.Contains(distributed.Distributions, row => row.LocationId == mumbai.Id);
        Assert.All(distributed.Distributions.Where(row => row.ProviderCode == "GOOGLE"), row =>
        {
            Assert.NotEqual("Published", row.Status);
            Assert.False(string.IsNullOrWhiteSpace(row.IdempotencyKey));
        });
    }

    [Fact]
    public async Task Foreign_location_is_rejected()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var item = await ApproveArticleAsync(db, tenant, user, search, business.Id);
        var error = await Assert.ThrowsAsync<AppException>(() =>
            new DistributeHubContentHandler(db, tenant).Handle(
                business.Id,
                item.Id,
                new DistributeHubContentRequest("GOOGLE", Guid.NewGuid()),
                CancellationToken.None));
        Assert.Contains("location", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Publish_everywhere_requires_a_plan_and_does_not_invent_external_posts()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var item = await ApproveArticleAsync(db, tenant, user, search, business.Id);
        var blocked = await Assert.ThrowsAsync<AppException>(() =>
            new PublishEverywhereHubContentHandler(db, tenant).Handle(
                business.Id, item.Id, new PublishEverywhereRequest(), CancellationToken.None));
        Assert.Contains("plan", blocked.Message, StringComparison.OrdinalIgnoreCase);

        var plan = SubscriptionPlan.Create("GROWTH", "Growth", 6999m, 3, false, 2, maxLocations: 5);
        db.Plans.Add(plan);
        db.Subscriptions.Add(Subscription.Start(tenant.RequireTenantId(), plan.Id));
        await db.SaveChangesAsync();

        var published = await new PublishEverywhereHubContentHandler(db, tenant).Handle(
            business.Id, item.Id, new PublishEverywhereRequest(), CancellationToken.None);
        Assert.Contains(published.Distributions, row => row.ProviderCode == "HUB" && row.Status == "Published");
        Assert.Contains(published.Distributions, row => row.ProviderCode == "WEBSITE" && row.Status != "Published");
        Assert.Contains(published.Distributions, row => row.ProviderCode == "GOOGLE");
        Assert.DoesNotContain(published.Distributions, row => row.ProviderCode == "WHATSAPP");
        Assert.DoesNotContain(published.Distributions, row => row.ProviderCode != "HUB" && row.Status == "Published");
    }

    [Fact]
    public async Task Retry_cancel_verify_and_idempotency_stay_honest()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var item = await ApproveArticleAsync(db, tenant, user, search, business.Id);
        var first = await new DistributeHubContentHandler(db, tenant).Handle(
            business.Id, item.Id, new DistributeHubContentRequest("INDIAMART", IdempotencyKey: "desk-1"), CancellationToken.None);
        var again = await new DistributeHubContentHandler(db, tenant).Handle(
            business.Id, item.Id, new DistributeHubContentRequest("INDIAMART", IdempotencyKey: "desk-1"), CancellationToken.None);
        Assert.Single(again.Distributions, row => row.ProviderCode == "INDIAMART");

        var held = first.Distributions.Single(row => row.ProviderCode == "INDIAMART");
        var retried = await new RetryHubDistributionHandler(db, tenant).Handle(business.Id, item.Id, held.Id, CancellationToken.None);
        Assert.Equal(2, retried.Distributions.Single(row => row.Id == held.Id).AttemptCount);
        Assert.NotEqual("Published", retried.Distributions.Single(row => row.Id == held.Id).Status);

        var verified = await new VerifyHubDistributionHandler(db, tenant).Handle(business.Id, item.Id, held.Id, CancellationToken.None);
        Assert.Equal("Hold", verified.Distributions.Single(row => row.Id == held.Id).VerificationStatus);

        var cancelled = await new CancelHubDistributionHandler(db, tenant).Handle(business.Id, item.Id, held.Id, CancellationToken.None);
        Assert.Equal("Cancelled", cancelled.Distributions.Single(row => row.Id == held.Id).Status);
    }

    private static async Task<HubContentResponse> ApproveArticleAsync(
        AppDbContext db,
        FixedTenantContext tenant,
        FixedUser user,
        InMemorySearchProvider search,
        Guid businessId)
    {
        var created = await new CreateHubContentHandler(db, tenant, user, search).Handle(
            businessId,
            new CreateHubContentRequest("ARTICLE", "Conference room AV", "conference-room-av", "A practical excerpt for the unique slug test.", "# Guide\n\nEnough body for the validator.", "Public", null, null, null, null, null),
            CancellationToken.None);
        await new SubmitHubApprovalHandler(db, tenant).Handle(businessId, created.Id, CancellationToken.None);
        return await new ApproveHubContentHandler(db, tenant).Handle(businessId, created.Id, CancellationToken.None);
    }

    private static async Task<(AppDbContext Db, FixedTenantContext Tenant, FixedUser User, InMemorySearchProvider Search, Business Business)> SeedAsync()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "AV Professionals", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pub-c-{Guid.NewGuid()}")
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
