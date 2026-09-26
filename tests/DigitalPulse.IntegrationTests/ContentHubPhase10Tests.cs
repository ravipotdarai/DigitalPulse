using DigitalPulse.Application.Features.Content;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class ContentHubPhase10Tests
{
    [Fact]
    public async Task Approve_requires_a_submitted_review()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var created = await new CreateHubContentHandler(db, tenant, user, search).Handle(
            business.Id,
            new CreateHubContentRequest("ARTICLE", "How to choose", "how-to-choose", "A practical excerpt for the unique slug test.", "# How\n\nEnough body for the validator.", "Public", null, null, null, null, null),
            CancellationToken.None);

        var blocked = await Assert.ThrowsAsync<Application.Common.AppException>(() =>
            new ApproveHubContentHandler(db, tenant).Handle(business.Id, created.Id, CancellationToken.None));
        Assert.Equal(400, blocked.StatusCode);

        var submitted = await new SubmitHubApprovalHandler(db, tenant).Handle(business.Id, created.Id, CancellationToken.None);
        Assert.Equal("PendingApproval", submitted.Status);
        Assert.True(await db.ApprovalRequests.AnyAsync(r => r.ContentItemId == created.Id && r.Open));

        var approved = await new ApproveHubContentHandler(db, tenant).Handle(business.Id, created.Id, CancellationToken.None);
        Assert.Equal("Approved", approved.Status);
        Assert.False(await db.ApprovalRequests.AnyAsync(r => r.ContentItemId == created.Id && r.Open));
        Assert.True(await db.ApprovalDecisions.AnyAsync());
    }

    private static async Task<(AppDbContext Db, FixedTenantContext Tenant, FixedUser User, InMemorySearchProvider Search, Business Business)> SeedAsync()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "A Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"hub-p10-{Guid.NewGuid()}")
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
