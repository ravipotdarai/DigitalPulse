using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Content;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Platforms;
using DigitalPulse.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class PublishingEnginePhaseHTests
{
    [Fact]
    public async Task End_to_end_desk_flow_never_invents_external_success()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var pune = BusinessLocation.Create(tenant.RequireTenantId(), business.Id, "Pune", null, "Pune", "MH", null, "IN");
        db.Locations.Add(pune);
        var plan = SubscriptionPlan.Create("GROWTH", "Growth", 6999m, 3, false, 2, maxLocations: 5);
        db.Plans.Add(plan);
        db.Subscriptions.Add(Subscription.Start(tenant.RequireTenantId(), plan.Id));
        await db.SaveChangesAsync();

        var created = await new CreateHubContentHandler(db, tenant, user, search).Handle(
            business.Id,
            new CreateHubContentRequest("ARTICLE", "Complete Conference Room AV Guide", "conference-room-av-guide", "A practical excerpt for the unique slug test.", "# Guide\n\nEnough body for the validator and a longer canonical article.", "Public", null, null, null, null, null),
            CancellationToken.None);
        await new SubmitHubApprovalHandler(db, tenant).Handle(business.Id, created.Id, CancellationToken.None);
        await new ApproveHubContentHandler(db, tenant).Handle(business.Id, created.Id, CancellationToken.None);
        var variants = await new CreateHubVariantsHandler(db, tenant).Handle(business.Id, created.Id, CancellationToken.None);
        Assert.Contains(variants.Variants, item => item.Kind == "GooglePost" && item.Body != created.Body);

        var everywhere = await new PublishEverywhereHubContentHandler(db, tenant).Handle(
            business.Id, created.Id, new PublishEverywhereRequest([pune.Id]), CancellationToken.None);
        Assert.Contains(everywhere.Distributions, item => item.ProviderCode == "HUB" && item.Status == "Published");
        Assert.Contains(everywhere.Distributions, item => item.ProviderCode == "GOOGLE" && item.LocationId == pune.Id && item.Status != "Published");
        Assert.Contains(everywhere.Distributions, item => item.ProviderCode == "WEBSITE" && item.Status != "Published");
        Assert.True(new WebsiteAdapter().Describe().Capabilities.AssistedOnly);

        var foreign = await Assert.ThrowsAsync<AppException>(() =>
            new DistributeHubContentHandler(db, tenant).Handle(
                business.Id, created.Id, new DistributeHubContentRequest("GOOGLE", Guid.NewGuid()), CancellationToken.None));
        Assert.Contains("location", foreign.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<(AppDbContext Db, FixedTenantContext Tenant, FixedUser User, InMemorySearchProvider Search, Business Business)> SeedAsync()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "AV Professionals", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pub-h-{Guid.NewGuid()}")
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
