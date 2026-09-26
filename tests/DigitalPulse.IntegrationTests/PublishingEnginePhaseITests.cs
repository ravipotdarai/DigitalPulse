using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Content;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Content;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class PublishingEnginePhaseITests
{
    [Fact]
    public async Task Distributions_are_tenant_and_business_isolated()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var businessA = Business.Create(tenantA, "A Co", null);
        var businessB = Business.Create(tenantB, "B Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pub-i-{Guid.NewGuid()}")
            .Options;
        await using (var seed = new AppDbContext(options, tenantContext: null))
        {
            seed.Businesses.AddRange(businessA, businessB);
            seed.ContentDistributions.Add(ContentDistribution.Start(tenantA, businessA.Id, Guid.NewGuid(), "GOOGLE", null, null, Guid.NewGuid(), "a-key"));
            seed.ContentDistributions.Add(ContentDistribution.Start(tenantB, businessB.Id, Guid.NewGuid(), "GOOGLE", null, null, Guid.NewGuid(), "b-key"));
            await seed.SaveChangesAsync();
        }

        await using var dbA = new AppDbContext(options, new FixedTenantContext(tenantA));
        var visible = await dbA.ContentDistributions.Select(row => row.IdempotencyKey).ToListAsync();
        Assert.Equal(["a-key"], visible);

        var user = new FixedUser();
        var created = await new CreateHubContentHandler(dbA, new FixedTenantContext(tenantA), user, new InMemorySearchProvider()).Handle(
            businessA.Id,
            new CreateHubContentRequest("ARTICLE", "Isolated article", "isolated-article", "A practical excerpt for the unique slug test.", "# Body\n\nEnough body for the validator.", "Public", null, null, null, null, null),
            CancellationToken.None);
        await new SubmitHubApprovalHandler(dbA, new FixedTenantContext(tenantA)).Handle(businessA.Id, created.Id, CancellationToken.None);
        await new ApproveHubContentHandler(dbA, new FixedTenantContext(tenantA)).Handle(businessA.Id, created.Id, CancellationToken.None);

        var missing = await Assert.ThrowsAsync<AppException>(() =>
            new DistributeHubContentHandler(dbA, new FixedTenantContext(tenantA)).Handle(
                businessB.Id, created.Id, "HUB", CancellationToken.None));
        Assert.True(missing.StatusCode is 403 or 404);
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
