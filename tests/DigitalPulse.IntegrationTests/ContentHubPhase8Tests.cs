using DigitalPulse.Application.Features.Content;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Projects;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class ContentHubPhase8Tests
{
    [Fact]
    public async Task Case_study_is_assembled_from_the_stored_project()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var project = Project.Create(tenant.RequireTenantId(), business.Id, "Harbour Roast", "Cafe", null, "Pune", "Recorded install.", "Lights stayed on.", null, null, ProjectPermissionScope.None, ProjectConfidentiality.Internal);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var created = await new CreateHubCaseStudyHandler(db, tenant, user, search).Handle(business.Id, project.Id, CancellationToken.None);
        Assert.Equal("CASE_STUDY", created.ContentTypeCode);
        Assert.Equal(project.Id, created.ProjectId);
        Assert.Contains("Harbour Roast", created.Title);
        Assert.Contains("Cafe", created.Body);
        Assert.Contains("Pune", created.Body);
        Assert.DoesNotContain("invented", created.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Hold", created.Status);

        var again = await new CreateHubCaseStudyHandler(db, tenant, user, search).Handle(business.Id, project.Id, CancellationToken.None);
        Assert.Equal(created.Id, again.Id);

        var workspace = await new GetContentHubHandler(db, tenant).Handle(business.Id, CancellationToken.None);
        Assert.Contains(workspace.Items, item => item.Id == created.Id);
        Assert.Contains(workspace.Projects, item => item.Id == project.Id && item.Name == "Harbour Roast");
    }

    private static async Task<(AppDbContext Db, FixedTenantContext Tenant, FixedUser User, InMemorySearchProvider Search, Business Business)> SeedAsync()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "A Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"hub-p8-{Guid.NewGuid()}")
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
