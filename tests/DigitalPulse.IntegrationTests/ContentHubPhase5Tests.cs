using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Ai;
using DigitalPulse.Application.Features.Content;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Domain.Ai;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Projects;
using DigitalPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class ContentHubPhase5Tests
{
    [Fact]
    public async Task Assist_does_not_write_an_article()
    {
        var (db, tenant, business) = await SeedAsync();
        var before = await db.ContentItems.CountAsync();
        var result = await new AssistHubContentHandler(db, tenant, new HoldOrchestrator()).Handle(
            business.Id,
            new AssistHubContentRequest("outline", "Conference room guide", null, null),
            CancellationToken.None);
        Assert.Equal("outline", result.Action);
        Assert.Equal("body", result.Target);
        Assert.False(result.IsLive);
        Assert.Contains("assembled", result.Hold, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(business.Name, result.Suggestion);
        Assert.Equal(before, await db.ContentItems.CountAsync());
    }

    [Fact]
    public async Task Assist_rejects_unknown_actions()
    {
        var (db, tenant, business) = await SeedAsync();
        var error = await Assert.ThrowsAsync<Application.Common.AppException>(() =>
            new AssistHubContentHandler(db, tenant, new HoldOrchestrator()).Handle(
                business.Id,
                new AssistHubContentRequest("autopilot", "Write it", null, null),
                CancellationToken.None));
        Assert.Equal(400, error.StatusCode);
    }

    [Fact]
    public async Task Assist_outline_draft_rewrite_shorten_expand_faq_are_structurally_different()
    {
        var (db, tenant, business) = await SeedAsync();
        var handler = new AssistHubContentHandler(db, tenant, new HoldOrchestrator());
        var section = "# Conference rooms\n\nA long draft about conference rooms for A Co.\nSecond paragraph stays in the editor.\nThird paragraph is still here.\nFourth paragraph is still here.\nFifth paragraph is still here.";
        var outline = await handler.Handle(business.Id, new AssistHubContentRequest("outline", "Conference room guide", null, section), CancellationToken.None);
        var draft = await handler.Handle(business.Id, new AssistHubContentRequest("draft", "Conference room guide", null, section), CancellationToken.None);
        var rewrite = await handler.Handle(business.Id, new AssistHubContentRequest("rewrite", "Conference room guide", null, section), CancellationToken.None);
        var shorten = await handler.Handle(business.Id, new AssistHubContentRequest("shorten", "Conference room guide", null, section), CancellationToken.None);
        var expand = await handler.Handle(business.Id, new AssistHubContentRequest("expand", "Conference room guide", null, section), CancellationToken.None);
        var faq = await handler.Handle(business.Id, new AssistHubContentRequest("faq", "Conference room guide", null, section), CancellationToken.None);

        Assert.All(new[] { outline, draft, rewrite, shorten, expand, faq }, item => Assert.Equal("body", item.Target));
        var bodies = new[] { outline.Suggestion, draft.Suggestion, rewrite.Suggestion, shorten.Suggestion, expand.Suggestion, faq.Suggestion };
        Assert.Equal(bodies.Length, bodies.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("# Outline:", outline.Suggestion);
        Assert.Contains("1. Open with", outline.Suggestion);
        Assert.DoesNotContain("## Approved facts", outline.Suggestion);
        Assert.Contains("## Approved facts", draft.Suggestion);
        Assert.Contains("# Rewrite of", rewrite.Suggestion);
        Assert.Contains("Keep only these recorded claims", rewrite.Suggestion);
        Assert.Contains("# Short version of", shorten.Suggestion);
        Assert.Contains("A long draft about conference rooms", shorten.Suggestion);
        Assert.DoesNotContain("## Approved facts", shorten.Suggestion);
        Assert.Contains("# Expanded:", expand.Suggestion);
        Assert.Contains("## How-to from stored services", expand.Suggestion);
        Assert.Contains("# FAQ:", faq.Suggestion);
        Assert.Contains("What is Conference room guide?", faq.Suggestion);
        Assert.Contains("Conference rooms", draft.Suggestion);
        Assert.Contains("Harbour Roast", expand.Suggestion);
        Assert.Contains("Pune", faq.Suggestion);
    }

    [Fact]
    public async Task Assist_platform_actions_append_including_youtube()
    {
        var (db, tenant, business) = await SeedAsync();
        var handler = new AssistHubContentHandler(db, tenant, new HoldOrchestrator());
        foreach (var action in new[] { "social", "linkedin", "google", "instagram", "youtube" })
        {
            var result = await handler.Handle(business.Id, new AssistHubContentRequest(action, "Conference room guide", null, null), CancellationToken.None);
            Assert.Equal("append", result.Target);
            Assert.Contains(business.Name, result.Suggestion);
        }

        var youtube = await handler.Handle(business.Id, new AssistHubContentRequest("youtube", "Conference room guide", null, null), CancellationToken.None);
        Assert.Contains("# YouTube script:", youtube.Suggestion);
        Assert.Contains("Beats", youtube.Suggestion);
        Assert.DoesNotContain("## Approved facts", youtube.Suggestion);
    }

    private static async Task<(AppDbContext Db, FixedTenantContext Tenant, Business Business)> SeedAsync()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "A Co", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"hub-p5-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options, new FixedTenantContext(tenantId));
        db.Businesses.Add(business);
        db.Facts.Add(BusinessFact.Create(tenantId, business.Id, "LOCATION", "Pune", FactStatus.Approved));
        db.Services.Add(Service.Create(tenantId, business.Id, "Conference rooms", null));
        db.Projects.Add(Project.Create(tenantId, business.Id, "Harbour Roast", null, null, null, null, null, null, null, ProjectPermissionScope.None, ProjectConfidentiality.Internal));
        await db.SaveChangesAsync();
        return (db, new FixedTenantContext(tenantId), business);
    }

    private sealed class HoldOrchestrator : IAiOrchestrator
    {
        public Task<AiOrchestrationResult> RunAsync(AiOrchestrationRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new AiOrchestrationResult(
                "",
                "Development",
                false,
                "hold",
                "content.v1",
                request.Ask,
                new AiValidationResult(true, true, false, false, AiConfidence.Low, AiRunStatus.Held, "held"),
                null,
                null,
                null));
    }

    private sealed class FixedTenantContext : ITenantContext
    {
        public FixedTenantContext(Guid tenantId) => TenantId = tenantId;
        public Guid? TenantId { get; }
        public Guid RequireTenantId() => TenantId!.Value;
    }
}
