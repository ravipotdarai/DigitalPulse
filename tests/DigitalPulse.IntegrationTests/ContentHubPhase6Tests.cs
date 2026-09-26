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

public sealed class ContentHubPhase6Tests
{
    [Fact]
    public async Task Analyze_scores_named_health_and_stored_entities()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var created = await new CreateHubContentHandler(db, tenant, user, search).Handle(
            business.Id,
            new CreateHubContentRequest(
                "ARTICLE",
                "How to design conference rooms",
                "how-to-design-conference-rooms",
                "A practical excerpt for the hub checklist snippet.",
                "# How to design\n\nConference rooms in Pune. Harbour Roast is a recorded project.\n\nSee [services](/hub/demo/rooms).\n\nFAQ\nWhat should buyers ask?\n1. Room size.\n2. Display.\n\n## Definition\nA conference room is a stored space.\n## Comparison\nThis vs that.\n## Key facts\nKey considerations stay stored.\n## Summary\nIn summary, use the record.",
                "Public",
                null,
                "conference",
                "https://example.com/guide",
                null,
                null,
                null,
                "How to design conference rooms",
                "A stored meta description that is long enough for a search snippet on this business."),
            CancellationToken.None);

        var analyzed = await new AnalyzeHubSeoHandler(db, tenant).Handle(business.Id, created.Id, "conference", CancellationToken.None);
        Assert.Equal("Informational", analyzed.Seo.SearchIntent);
        Assert.Equal(ContentSeo.HealthTotal, analyzed.Seo.ChecksTotal);
        Assert.Equal(analyzed.Seo.ChecksPassed, analyzed.Seo.Checks.Count(item => item.Passed));
        Assert.Equal((int)Math.Round(100d * analyzed.Seo.ChecksPassed / analyzed.Seo.ChecksTotal), analyzed.Seo.SeoScore);
        Assert.NotEqual(87, analyzed.Seo.SeoScore);
        Assert.Equal(100, analyzed.Seo.EntityCoverageScore);
        Assert.Equal(3, analyzed.Seo.EntitiesTotal);
        Assert.Contains(analyzed.Seo.Checks, item => item.Code == "entity-coverage" && item.Passed);
        Assert.Contains(analyzed.Seo.Checks, item => item.Code == "search-intent" && item.Passed);
        Assert.Equal(ContentSeo.AeoTotal, analyzed.Seo.AeoChecks.Count);
        Assert.Equal(100, analyzed.Seo.AeoScore);
        Assert.NotEqual(87, analyzed.Seo.AeoScore);
        Assert.DoesNotContain(analyzed.Seo.Notes, note => note.Contains("Graphify", StringComparison.OrdinalIgnoreCase));

        var workspace = await new GetContentHubHandler(db, tenant).Handle(business.Id, CancellationToken.None);
        Assert.Contains("Conference rooms", workspace.Entities);
        Assert.Contains("Harbour Roast", workspace.Entities);
        Assert.Contains("Pune", workspace.Entities);
    }

    [Fact]
    public async Task Analyze_keeps_entity_coverage_at_zero_when_records_are_unnamed()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var created = await new CreateHubContentHandler(db, tenant, user, search).Handle(
            business.Id,
            new CreateHubContentRequest(
                "ARTICLE",
                "How to write a thin note",
                "how-to-write-a-thin-note",
                "A practical excerpt for the hub checklist snippet.",
                "# How\n\nThis draft never names the stored service or project.",
                "Public",
                null,
                "write",
                null,
                null,
                null),
            CancellationToken.None);

        Assert.Equal(0, created.Seo.EntityCoverageScore);
        Assert.Contains(created.Seo.Checks, item => item.Code == "entity-coverage" && !item.Passed);
        Assert.NotEqual(87, created.Seo.SeoScore);
    }

    private static async Task<(AppDbContext Db, FixedTenantContext Tenant, FixedUser User, InMemorySearchProvider Search, Business Business)> SeedAsync()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "A Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"hub-p6-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options, new FixedTenantContext(tenantId));
        db.Businesses.Add(business);
        db.Facts.Add(BusinessFact.Create(tenantId, business.Id, "LOCATION", "Pune", FactStatus.Approved));
        db.Services.Add(Service.Create(tenantId, business.Id, "Conference rooms", null));
        db.Projects.Add(Project.Create(tenantId, business.Id, "Harbour Roast", null, null, null, null, null, null, null, ProjectPermissionScope.None, ProjectConfidentiality.Internal));
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
