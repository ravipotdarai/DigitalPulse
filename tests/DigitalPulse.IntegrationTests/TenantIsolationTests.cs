using DigitalPulse.Domain.Ai;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Platforms;
using DigitalPulse.Domain.Scans;
using DigitalPulse.Domain.Tenancy;
using DigitalPulse.Domain.Directories;
using DigitalPulse.Domain.Projects;
using DigitalPulse.Domain.Social;
using DigitalPulse.Domain.Website;
using DigitalPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class TenantIsolationTests
{
    [Fact]
    public async Task Query_filter_hides_other_tenant_businesses()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"iso-{Guid.NewGuid()}")
            .Options;

        await using (var seed = new AppDbContext(options, tenantContext: null))
        {
            seed.Businesses.Add(Business.Create(tenantA, "A Co", null));
            seed.Businesses.Add(Business.Create(tenantB, "B Co", null));
            await seed.SaveChangesAsync();
        }

        var contextA = new FixedTenantContext(tenantA);
        await using var dbA = new AppDbContext(options, contextA);
        var visible = await dbA.Businesses.Select(b => b.Name).ToListAsync();
        Assert.Equal(["A Co"], visible);
    }

    [Fact]
    public async Task Query_filter_hides_other_tenant_connections()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var businessA = Business.Create(tenantA, "A Co", null);
        var businessB = Business.Create(tenantB, "B Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"iso-conn-{Guid.NewGuid()}")
            .Options;

        await using (var seed = new AppDbContext(options, tenantContext: null))
        {
            seed.Businesses.AddRange(businessA, businessB);
            seed.Connections.Add(PlatformConnection.Start(tenantA, businessA.Id, "GOOGLE", PlatformAuthMode.OAuth, "state-a"));
            seed.Connections.Add(PlatformConnection.Start(tenantB, businessB.Id, "GOOGLE", PlatformAuthMode.OAuth, "state-b"));
            await seed.SaveChangesAsync();
        }

        await using var dbA = new AppDbContext(options, new FixedTenantContext(tenantA));
        var visible = await dbA.Connections.Select(c => c.AuthorizationState).ToListAsync();
        Assert.Equal(["state-a"], visible);
    }

    [Fact]
    public async Task Query_filter_hides_other_tenant_scans()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var businessA = Business.Create(tenantA, "A Co", null);
        var businessB = Business.Create(tenantB, "B Co", null);
        var scanA = Scan.Start(tenantA, businessA.Id);
        var scanB = Scan.Start(tenantB, businessB.Id);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"iso-scan-{Guid.NewGuid()}")
            .Options;

        await using (var seed = new AppDbContext(options, tenantContext: null))
        {
            seed.Businesses.AddRange(businessA, businessB);
            seed.Scans.AddRange(scanA, scanB);
            seed.Findings.Add(Finding.Open(
                tenantA, scanA.Id, businessA.Id, "Identity", FindingSeverity.High,
                "Only A", "A evidence", "expected", "observed", "rec", "act", "verify", FindingAutomationState.Suggested));
            await seed.SaveChangesAsync();
        }

        await using var dbA = new AppDbContext(options, new FixedTenantContext(tenantA));
        var visibleScans = await dbA.Scans.Select(s => s.BusinessId).ToListAsync();
        var visibleFindings = await dbA.Findings.Select(f => f.Title).ToListAsync();
        Assert.Equal([businessA.Id], visibleScans);
        Assert.Equal(["Only A"], visibleFindings);
    }

    [Fact]
    public async Task Query_filter_hides_other_tenant_website_snapshots()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var businessA = Business.Create(tenantA, "A Co", "https://a.example");
        var businessB = Business.Create(tenantB, "B Co", "https://b.example");
        var snapA = WebsiteSnapshot.Record(
            tenantA, businessA.Id, "https://a.example/", WebsiteFetchStatus.Reached, 200,
            "A", null, null, null, null, false, false, false, false, 10, true, false, null);
        var snapB = WebsiteSnapshot.Record(
            tenantB, businessB.Id, "https://b.example/", WebsiteFetchStatus.Reached, 200,
            "B", null, null, null, null, false, false, false, false, 10, true, false, null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"iso-web-{Guid.NewGuid()}")
            .Options;

        await using (var seed = new AppDbContext(options, tenantContext: null))
        {
            seed.Businesses.AddRange(businessA, businessB);
            seed.WebsiteSnapshots.AddRange(snapA, snapB);
            await seed.SaveChangesAsync();
        }

        await using var dbA = new AppDbContext(options, new FixedTenantContext(tenantA));
        var visible = await dbA.WebsiteSnapshots.Select(s => s.Title).ToListAsync();
        Assert.Equal(["A"], visible);
    }

    [Fact]
    public async Task Query_filter_hides_other_tenant_social_content()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var businessA = Business.Create(tenantA, "A Co", null);
        var businessB = Business.Create(tenantB, "B Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"iso-social-{Guid.NewGuid()}")
            .Options;

        await using (var seed = new AppDbContext(options, tenantContext: null))
        {
            seed.Businesses.AddRange(businessA, businessB);
            seed.SocialContent.Add(SocialContentItem.Draft(tenantA, businessA.Id, "FACEBOOK", SocialContentKind.FacebookPost, "A", "A body"));
            seed.SocialContent.Add(SocialContentItem.Draft(tenantB, businessB.Id, "FACEBOOK", SocialContentKind.FacebookPost, "B", "B body"));
            await seed.SaveChangesAsync();
        }

        await using var dbA = new AppDbContext(options, new FixedTenantContext(tenantA));
        var visible = await dbA.SocialContent.Select(c => c.Title).ToListAsync();
        Assert.Equal(["A"], visible);
    }

    [Fact]
    public async Task Query_filter_hides_other_tenant_directory_tasks()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var businessA = Business.Create(tenantA, "A Co", null);
        var businessB = Business.Create(tenantB, "B Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"iso-dir-{Guid.NewGuid()}")
            .Options;

        await using (var seed = new AppDbContext(options, tenantContext: null))
        {
            seed.Businesses.AddRange(businessA, businessB);
            var taskA = DirectoryTask.Prepare(tenantA, businessA.Id, "INDIAMART", "A Co", null, null, null, null, [("One", "A")]);
            var taskB = DirectoryTask.Prepare(tenantB, businessB.Id, "INDIAMART", "B Co", null, null, null, null, [("One", "B")]);
            seed.DirectoryTasks.AddRange(taskA, taskB);
            await seed.SaveChangesAsync();
        }

        await using var dbA = new AppDbContext(options, new FixedTenantContext(tenantA));
        var visible = await dbA.DirectoryTasks.Select(t => t.PreparedName).ToListAsync();
        Assert.Equal(["A Co"], visible);
    }

    [Fact]
    public async Task Query_filter_hides_other_tenant_projects()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var businessA = Business.Create(tenantA, "A Co", null);
        var businessB = Business.Create(tenantB, "B Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"iso-project-{Guid.NewGuid()}")
            .Options;

        await using (var seed = new AppDbContext(options, tenantContext: null))
        {
            seed.Businesses.AddRange(businessA, businessB);
            seed.Projects.Add(Project.Create(tenantA, businessA.Id, "A Project", null, null, null, null, null, null, null, ProjectPermissionScope.None, ProjectConfidentiality.Internal));
            seed.Projects.Add(Project.Create(tenantB, businessB.Id, "B Project", null, null, null, null, null, null, null, ProjectPermissionScope.None, ProjectConfidentiality.Internal));
            await seed.SaveChangesAsync();
        }

        await using var dbA = new AppDbContext(options, new FixedTenantContext(tenantA));
        var visible = await dbA.Projects.Select(p => p.Name).ToListAsync();
        Assert.Equal(["A Project"], visible);
    }

    [Fact]
    public async Task Query_filter_hides_other_tenant_knowledge()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var businessA = Business.Create(tenantA, "A Co", null);
        var businessB = Business.Create(tenantB, "B Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"iso-ai-{Guid.NewGuid()}")
            .Options;

        await using (var seed = new AppDbContext(options, tenantContext: null))
        {
            seed.Businesses.AddRange(businessA, businessB);
            seed.KnowledgeEntries.Add(KnowledgeEntry.Create(tenantA, businessA.Id, "A note", "Only A", KnowledgeKind.Note, null));
            seed.KnowledgeEntries.Add(KnowledgeEntry.Create(tenantB, businessB.Id, "B note", "Only B", KnowledgeKind.Note, null));
            await seed.SaveChangesAsync();
        }

        await using var dbA = new AppDbContext(options, new FixedTenantContext(tenantA));
        var visible = await dbA.KnowledgeEntries.Select(k => k.Title).ToListAsync();
        Assert.Equal(["A note"], visible);
    }

    private sealed class FixedTenantContext : Application.Abstractions.ITenantContext
    {
        public FixedTenantContext(Guid tenantId) => TenantId = tenantId;
        public Guid? TenantId { get; }
        public Guid RequireTenantId() => TenantId!.Value;
    }
}
