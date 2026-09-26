using DigitalPulse.Application.Ai;
using DigitalPulse.Application.Features.Social;
using DigitalPulse.Contracts.Social;
using DigitalPulse.Domain.Ai;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Social;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class AiPhase9FeatureTests
{
    [Fact]
    public async Task Google_post_generation_stays_assisted_and_is_not_posted()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "AV Professionals", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ai-p9-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options, new FixedTenant(tenantId));
        db.Businesses.Add(business);
        db.Facts.Add(BusinessFact.Create(tenantId, business.Id, "NAME", "AV Professionals", FactStatus.Approved));
        db.GraphNodes.Add(GraphNode.Create(tenantId, business.Id, GraphNodeKind.Project, "Corporate Conference Room Installation", "project:1", "Installed a conference room."));
        await db.SaveChangesAsync();

        var handler = new GenerateSocialDraftHandler(
            db,
            new FixedTenant(tenantId),
            new AiContextBuilder(db, new InMemorySearchProvider()),
            new HoldOrchestrator());
        var draft = await handler.Handle(
            business.Id,
            new GenerateSocialDraftRequest("GOOGLE", "Generate a Google Business Profile post."),
            CancellationToken.None);

        Assert.Equal("GOOGLE", draft.PlatformCode);
        Assert.Equal(SocialContentKind.GooglePost.ToString(), draft.Kind);
        Assert.Equal(SocialContentStatus.Assisted.ToString(), draft.Status);
        Assert.Contains("Nothing was posted", draft.VerificationDetail ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Posted to Google", draft.Body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class HoldOrchestrator : IAiOrchestrator
    {
        public Task<AiOrchestrationResult> RunAsync(AiOrchestrationRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new AiOrchestrationResult(
                "AV Professionals completed a conference room installation.",
                "Development",
                false,
                "hold",
                "social.v1",
                request.Ask,
                new AiValidationResult(true, true, false, false, AiConfidence.Low, AiRunStatus.Held, "held"),
                null,
                null,
                null));
    }

    private sealed class FixedTenant : Application.Abstractions.ITenantContext
    {
        public FixedTenant(Guid tenantId) => TenantId = tenantId;
        public Guid? TenantId { get; }
        public Guid RequireTenantId() => TenantId!.Value;
    }
}
