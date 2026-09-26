using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Features.Content;
using DigitalPulse.Contracts.Content;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Platforms;
using DigitalPulse.Infrastructure.Persistence;
using DigitalPulse.Infrastructure.Platforms;
using DigitalPulse.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalPulse.IntegrationTests;

public sealed class PublishingEnginePhaseDTests
{
    [Fact]
    public async Task Website_and_social_adapters_stay_capability_driven()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var item = await ApproveAsync(db, tenant, user, search, business.Id);
        var catalog = new ScriptedCatalog(
            new WebsiteAdapter(),
            new ScriptedAdapter("LINKEDIN", new PlatformCapabilities(true, true, true, false, true, true, false), new PlatformPublishResult("Hold", "LinkedIn grant missing.")),
            new ScriptedAdapter("FACEBOOK", new PlatformCapabilities(true, true, true, false, true, true, false), new PlatformPublishResult("Hold", "Facebook grant missing.")),
            new ScriptedAdapter("INSTAGRAM", new PlatformCapabilities(true, true, true, false, true, true, false), new PlatformPublishResult("Hold", "Instagram grant missing.")),
            new ScriptedAdapter("YOUTUBE", new PlatformCapabilities(true, true, false, false, true, true, false), new PlatformPublishResult("Hold", "YouTube grant missing.")));

        var workspace = await new GetContentHubHandler(db, tenant, catalog).Handle(business.Id, CancellationToken.None);
        Assert.Contains(workspace.Channels, row => row.ProviderCode == "WEBSITE" && row.Mode == "Assisted");
        Assert.Contains(workspace.Channels, row => row.ProviderCode == "LINKEDIN" && row.Mode == "Assisted");
        Assert.Contains(workspace.Channels, row => row.ProviderCode == "FACEBOOK" && row.Mode == "Assisted");
        Assert.Contains(workspace.Channels, row => row.ProviderCode == "INSTAGRAM" && row.Mode == "Assisted");
        Assert.Contains(workspace.Channels, row => row.ProviderCode == "YOUTUBE" && row.Mode == "Assisted");

        foreach (var code in new[] { "WEBSITE", "LINKEDIN", "FACEBOOK", "INSTAGRAM", "YOUTUBE" })
        {
            var result = await new DistributeHubContentHandler(db, tenant, catalog).Handle(business.Id, item.Id, code, CancellationToken.None);
            Assert.Contains(result.Distributions, row => row.ProviderCode == code && row.Status != "Published");
        }
    }

    [Fact]
    public async Task Official_publish_is_recorded_only_when_the_adapter_confirms_it()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var item = await ApproveAsync(db, tenant, user, search, business.Id);
        var live = PlatformConnection.Start(tenant.RequireTenantId(), business.Id, "LINKEDIN", PlatformAuthMode.OAuth, "state-li");
        live.AttachLiveGrant("OAuth", "ref-1", "urn:li:person:1", "access-1", null, DateTimeOffset.UtcNow.AddHours(1), "w_member_social");
        db.Connections.Add(live);
        await db.SaveChangesAsync();

        var hold = await new DistributeHubContentHandler(db, tenant, new ScriptedCatalog(
            new ScriptedAdapter("LINKEDIN", PublishCaps(), new PlatformPublishResult("Hold", "Official LinkedIn write returned 400."))))
            .Handle(business.Id, item.Id, new DistributeHubContentRequest("LINKEDIN", IdempotencyKey: "li-hold"), CancellationToken.None);
        Assert.Contains(hold.Distributions, row => row.ProviderCode == "LINKEDIN" && row.Status != "Published");

        var published = await new DistributeHubContentHandler(db, tenant, new ScriptedCatalog(
            new ScriptedAdapter("LINKEDIN", PublishCaps(), new PlatformPublishResult("Published", "urn:li:share:99"))))
            .Handle(business.Id, item.Id, new DistributeHubContentRequest("LINKEDIN", IdempotencyKey: "li-ok"), CancellationToken.None);
        Assert.Contains(published.Distributions, row => row.ProviderCode == "LINKEDIN" && row.Status == "Published" && row.VerificationStatus == "Verified");
    }

    [Fact]
    public async Task Google_does_not_publish_without_a_locations_path()
    {
        var (db, tenant, user, search, business) = await SeedAsync();
        var item = await ApproveAsync(db, tenant, user, search, business.Id);
        var google = PlatformConnection.Start(tenant.RequireTenantId(), business.Id, "GOOGLE", PlatformAuthMode.OAuth, "state-g");
        google.AttachLiveGrant("OAuth", "ref-g", "accounts/123", "access-g", null, DateTimeOffset.UtcNow.AddHours(1), "business.manage");
        db.Connections.Add(google);
        await db.SaveChangesAsync();

        var result = await new DistributeHubContentHandler(db, tenant, new ScriptedCatalog(new GoogleAdapter(new RejectingGateway())))
            .Handle(business.Id, item.Id, "GOOGLE", CancellationToken.None);
        Assert.Contains(result.Distributions, row => row.ProviderCode == "GOOGLE" && row.Status != "Published");
    }

    private static PlatformCapabilities PublishCaps() => new(true, true, true, false, true, true, false);

    private static async Task<HubContentResponse> ApproveAsync(
        AppDbContext db, FixedTenantContext tenant, FixedUser user, InMemorySearchProvider search, Guid businessId)
    {
        var created = await new CreateHubContentHandler(db, tenant, user, search).Handle(
            businessId,
            new CreateHubContentRequest("ARTICLE", "Capability article", "capability-article", "A practical excerpt for the unique slug test.", "# Body\n\nEnough body for the validator.", "Public", null, null, null, null, null),
            CancellationToken.None);
        await new SubmitHubApprovalHandler(db, tenant).Handle(businessId, created.Id, CancellationToken.None);
        return await new ApproveHubContentHandler(db, tenant).Handle(businessId, created.Id, CancellationToken.None);
    }

    private static async Task<(AppDbContext Db, FixedTenantContext Tenant, FixedUser User, InMemorySearchProvider Search, Business Business)> SeedAsync()
    {
        var tenantId = Guid.NewGuid();
        var business = Business.Create(tenantId, "AV Professionals", null);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"pub-d-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options, new FixedTenantContext(tenantId));
        db.Businesses.Add(business);
        await db.SaveChangesAsync();
        return (db, new FixedTenantContext(tenantId), new FixedUser(), new InMemorySearchProvider(), business);
    }

    private sealed class ScriptedCatalog(params IPlatformAdapter[] adapters) : IPlatformAdapterCatalog
    {
        public IReadOnlyList<IPlatformAdapter> All() => adapters;
        public IPlatformAdapter Get(string code) => adapters.First(item => item.Describe().Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class ScriptedAdapter : IPlatformAdapter
    {
        private readonly PlatformDescriptor _descriptor;
        private readonly PlatformPublishResult _result;

        public ScriptedAdapter(string code, PlatformCapabilities capabilities, PlatformPublishResult result)
        {
            _descriptor = new PlatformDescriptor(code, code, "Social", PlatformAuthMode.OAuth, $"{code} official capability.", capabilities);
            _result = result;
        }

        public PlatformDescriptor Describe() => _descriptor;
        public Task<PlatformHealthResult> HealthCheckAsync(PlatformConnection connection, CancellationToken cancellationToken) =>
            Task.FromResult(new PlatformHealthResult("Hold", "scripted"));
        public Task<IReadOnlyList<PlatformDiagnostic>> DiagnoseAsync(PlatformConnection connection, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PlatformDiagnostic>>([]);
        public Task<PlatformPublishResult> PublishAsync(PlatformConnection connection, string title, string body, CancellationToken cancellationToken) =>
            Task.FromResult(_result);
        public Task<PlatformMetricsResult> MetricsAsync(PlatformConnection connection, CancellationToken cancellationToken) =>
            Task.FromResult(new PlatformMetricsResult("Hold", "scripted"));
    }

    private sealed class RejectingGateway : IOfficialPlatformGateway
    {
        public Task<OfficialHttpResult> SendAsync(
            HttpMethod method, string url, string? accessToken, string? jsonBody,
            IReadOnlyDictionary<string, string>? headers, CancellationToken cancellationToken) =>
            Task.FromResult(new OfficialHttpResult(400, "{}", false));

        public Task<OfficialHttpResult> SendMultipartAsync(
            HttpMethod method, string url, string? accessToken, IReadOnlyList<OfficialFormPart> parts,
            CancellationToken cancellationToken) =>
            Task.FromResult(new OfficialHttpResult(400, "{}", false));
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
