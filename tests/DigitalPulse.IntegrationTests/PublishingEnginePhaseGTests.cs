using DigitalPulse.Application.Features.Content;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class PublishingEnginePhaseGTests
{
    [Fact]
    public async Task Worker_jobs_release_due_articles_and_retry_holds_without_inventing_posts()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var item = await ApproveAsync(db, tenant, user, search, business.Id);
        await new ScheduleHubContentHandler(db, tenant, user).Handle(
            business.Id,
            item.Id,
            new ScheduleHubContentRequest(DateTimeOffset.UtcNow.AddMilliseconds(50), "HUB"),
            CancellationToken.None);
        await Task.Delay(80);
        var released = await ContentPublishingJobs.ReleaseDueAsync(db, business.Id, ignoreFilters: false, CancellationToken.None);
        Assert.Equal(1, released);
        await db.SaveChangesAsync();
        var published = await db.ContentItems.SingleAsync(row => row.Id == item.Id);
        Assert.Equal(DigitalPulse.Domain.Projects.ContentItemStatus.Published, published.Status);

        var held = await new DistributeHubContentHandler(db, tenant).Handle(business.Id, item.Id, "INDIAMART", CancellationToken.None);
        var retried = await ContentPublishingJobs.RetryHeldAsync(db, null, business.Id, false, CancellationToken.None);
        Assert.Equal(1, retried);
        await db.SaveChangesAsync();
        var row = held.Distributions.Single(item => item.ProviderCode == "INDIAMART");
        var fresh = await db.ContentDistributions.SingleAsync(item => item.Id == row.Id);
        Assert.Equal(2, fresh.AttemptCount);
        Assert.NotEqual(DigitalPulse.Domain.Content.ContentDistributionStatus.Published, fresh.Status);
    }

    private static async Task<HubContentResponse> ApproveAsync(
        AppDbContext db, FixedTenantContext tenant, FixedUser user, InMemorySearchProvider search, Guid businessId)
    {
        var created = await new CreateHubContentHandler(db, tenant, user, search).Handle(
            businessId,
            new CreateHubContentRequest("ARTICLE", "Scheduled AV", "scheduled-av", "A practical excerpt for the unique slug test.", "# Guide\n\nEnough body for the validator.", "Public", null, null, null, null, null),
            CancellationToken.None);
        await new SubmitHubApprovalHandler(db, tenant).Handle(businessId, created.Id, CancellationToken.None);
        return await new ApproveHubContentHandler(db, tenant).Handle(businessId, created.Id, CancellationToken.None);
    }

    private static async Task<(AppDbContext Db, FixedTenantContext Tenant, FixedUser User, InMemorySearchProvider Search, Business Business)> SeedAsync()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "AV Professionals", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pub-g-{Guid.NewGuid()}")
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
