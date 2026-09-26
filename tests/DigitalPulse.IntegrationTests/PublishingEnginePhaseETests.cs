using DigitalPulse.Application.Ai;
using DigitalPulse.Application.Features.Content;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Domain.Ai;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class PublishingEnginePhaseETests
{
    [Fact]
    public async Task Live_google_variant_uses_orchestrator_output_and_stays_shorter_than_the_article()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var created = await new CreateHubContentHandler(db, tenant, user, search).Handle(
            business.Id,
            new CreateHubContentRequest("ARTICLE", "Conference AV", "conference-av", "A practical excerpt for the unique slug test.", "# Guide\n\nEnough body for the validator and a longer canonical article than any Google post.", "Public", null, null, null, null, null),
            CancellationToken.None);
        var variants = await new CreateHubVariantsHandler(db, tenant, new EmptyContext(), new LiveOrchestrator("Short GBP post from stored facts."))
            .Handle(business.Id, created.Id, CancellationToken.None);
        var google = Assert.Single(variants.Variants, item => item.Kind == "GooglePost");
        Assert.Equal("Short GBP post from stored facts.", google.Body);
        Assert.True(google.Body.Length < created.Body.Length);
        Assert.Contains(variants.Variants, item => item.Kind == "WebsiteArticle" && item.Body == created.Body);
    }

    private static async Task<(AppDbContext Db, FixedTenantContext Tenant, FixedUser User, InMemorySearchProvider Search, Business Business)> SeedAsync()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "AV Professionals", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pub-e-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options, new FixedTenantContext(tenantId));
        db.Businesses.Add(business);
        await db.SaveChangesAsync();
        return (db, new FixedTenantContext(tenantId), new FixedUser(), new InMemorySearchProvider(), business);
    }

    private sealed class EmptyContext : IAiContextBuilder
    {
        public Task<AiBuiltContext> BuildAsync(Guid tenantId, Guid businessId, string ask, CancellationToken cancellationToken) =>
            Task.FromResult(new AiBuiltContext([], ["Business: AV Professionals"]));
    }

    private sealed class LiveOrchestrator(string output) : IAiOrchestrator
    {
        public Task<AiOrchestrationResult> RunAsync(AiOrchestrationRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new AiOrchestrationResult(
                output,
                "scripted",
                true,
                "dev",
                "v1",
                request.Ask,
                new AiValidationResult(true, true, false, false, AiConfidence.High, AiRunStatus.Completed, "ok"),
                null,
                null,
                null));
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
