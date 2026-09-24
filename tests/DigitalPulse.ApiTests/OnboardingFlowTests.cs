using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DigitalPulse.Contracts.WhatsApp;
using DigitalPulse.Contracts.Monitoring;
using DigitalPulse.Contracts.Actions;
using DigitalPulse.Contracts.Ai;
using DigitalPulse.Contracts.Auth;
using DigitalPulse.Contracts.Billing;
using DigitalPulse.Contracts.Businesses;
using DigitalPulse.Contracts.Connections;
using DigitalPulse.Contracts.Onboarding;
using DigitalPulse.Contracts.Scans;
using DigitalPulse.Contracts.Directories;
using DigitalPulse.Contracts.Projects;
using DigitalPulse.Contracts.Social;
using DigitalPulse.Contracts.Website;
using DigitalPulse.Contracts.Tenancy;
using DigitalPulse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DigitalPulse.ApiTests;

public sealed class DigitalPulseApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"dp-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Testing:UseInMemory", "true");
        builder.UseSetting("Testing:Database", _dbName);
    }
}

public sealed class OnboardingFlowTests : IClassFixture<DigitalPulseApiFactory>
{
    private readonly HttpClient _client;

    public OnboardingFlowTests(DigitalPulseApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Register_login_onboard_and_open_dashboard()
    {
        var email = $"owner-{Guid.NewGuid():N}@example.com";
        var register = await _client.PostAsJsonAsync("/v1/auth/register", new RegisterRequest(email, "Ravi", "Password1!"));
        register.EnsureSuccessStatusCode();
        var registered = await register.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(registered);
        UseToken(registered!.AccessToken);

        var login = await _client.PostAsJsonAsync("/v1/auth/login", new LoginRequest(email, "Password1!"));
        login.EnsureSuccessStatusCode();
        var session = await login.Content.ReadFromJsonAsync<AuthResponse>();
        UseToken(session!.AccessToken);

        var tenant = await _client.PostAsJsonAsync("/v1/tenants", new CreateTenantRequest("Harbour Studio", "Direct"));
        tenant.EnsureSuccessStatusCode();
        var afterTenant = await tenant.Content.ReadFromJsonAsync<AuthResponse>();
        UseToken(afterTenant!.AccessToken);
        Assert.NotNull(afterTenant.TenantId);

        var current = await _client.GetFromJsonAsync<TenantResponse>("/v1/tenants/current");
        Assert.Equal("Harbour Studio", current!.Name);

        var business = await _client.PostAsJsonAsync("/v1/businesses", new CreateBusinessRequest("Harbour Coffee", "https://harbour.example"));
        business.EnsureSuccessStatusCode();
        var createdBusiness = await business.Content.ReadFromJsonAsync<BusinessResponse>();

        var location = await _client.PostAsJsonAsync(
            $"/v1/businesses/{createdBusiness!.Id}/locations",
            new CreateLocationRequest("Flagship", "12 Marine Drive", "Mumbai", "MH", "400002", "IN"));
        location.EnsureSuccessStatusCode();

        var plans = await _client.GetFromJsonAsync<List<PlanResponse>>("/v1/plans");
        Assert.DoesNotContain(plans!, p => p.Code == "AGENCY");
        Assert.Contains(plans!, p => p.Code == "STARTER");

        var sub = await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        sub.EnsureSuccessStatusCode();

        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/v1/dashboard");
        Assert.Equal("Harbour Studio", dashboard!.TenantName);
        Assert.Equal("Starter", dashboard.PlanName);
        Assert.Equal(1, dashboard.BusinessCount);
    }

    [Fact]
    public async Task Can_update_tenant_business_and_location()
    {
        var email = $"owner-{Guid.NewGuid():N}@example.com";
        var register = await _client.PostAsJsonAsync("/v1/auth/register", new RegisterRequest(email, "Ravi", "Password1!"));
        register.EnsureSuccessStatusCode();
        var registered = await register.Content.ReadFromJsonAsync<AuthResponse>();
        UseToken(registered!.AccessToken);

        var tenant = await _client.PostAsJsonAsync("/v1/tenants", new CreateTenantRequest("Old Tenant", "Direct"));
        var afterTenant = await tenant.Content.ReadFromJsonAsync<AuthResponse>();
        UseToken(afterTenant!.AccessToken);

        var business = await _client.PostAsJsonAsync("/v1/businesses", new CreateBusinessRequest("Old Biz", null));
        var createdBusiness = await business.Content.ReadFromJsonAsync<BusinessResponse>();
        var location = await _client.PostAsJsonAsync(
            $"/v1/businesses/{createdBusiness!.Id}/locations",
            new CreateLocationRequest("Old Loc", null, "Pune", null, null, "IN"));
        var createdLocation = await location.Content.ReadFromJsonAsync<LocationResponse>();

        var renamedTenant = await _client.PutAsJsonAsync("/v1/tenants/current", new UpdateTenantRequest("New Tenant"));
        renamedTenant.EnsureSuccessStatusCode();
        var tenantBody = await renamedTenant.Content.ReadFromJsonAsync<TenantResponse>();
        Assert.Equal("New Tenant", tenantBody!.Name);

        var renamedBusiness = await _client.PutAsJsonAsync(
            $"/v1/businesses/{createdBusiness.Id}",
            new UpdateBusinessRequest("New Biz", "https://example.com"));
        renamedBusiness.EnsureSuccessStatusCode();

        var renamedLocation = await _client.PutAsJsonAsync(
            $"/v1/businesses/{createdBusiness.Id}/locations/{createdLocation!.Id}",
            new CreateLocationRequest("New Loc", "1 Main", "Mumbai", "MH", "400001", "IN"));
        renamedLocation.EnsureSuccessStatusCode();
        var locBody = await renamedLocation.Content.ReadFromJsonAsync<LocationResponse>();
        Assert.Equal("New Loc", locBody!.Name);
        Assert.Equal("Mumbai", locBody.City);
    }

    [Fact]
    public async Task Tenant_isolation_blocks_cross_tenant_business_access()
    {
        var userA = await RegisterAndOnboard("Direct", "Alpha Co");
        var userB = await RegisterAndOnboard("Direct", "Beta Co");

        UseToken(userA.Token);
        var businesses = await _client.GetFromJsonAsync<List<BusinessResponse>>("/v1/businesses");
        Assert.NotNull(businesses);
        Assert.Single(businesses);
        Assert.Equal("Alpha Co Biz", businesses.Single().Name);

        UseToken(userB.Token);
        var location = await _client.PostAsJsonAsync(
            $"/v1/businesses/{userA.BusinessId}/locations",
            new CreateLocationRequest("Hijack", null, null, null, null, "IN"));
        Assert.Equal(HttpStatusCode.NotFound, location.StatusCode);
    }

    [Fact]
    public async Task Can_build_business_identity_and_keep_restricted_facts_unpublishable()
    {
        var session = await RegisterAndOnboard("Direct", "Ledger Co");
        UseToken(session.Token);

        var profile = await _client.PutAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/profile",
            new UpdateBusinessProfileRequest("Ledger Cafe", "https://ledger.example", 2014, "Warm and exact.", "HOSPITALITY"));
        profile.EnsureSuccessStatusCode();

        var contact = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/contacts",
            new ContactPointRequest("Phone", "+91 22 1234 5678", "Front desk"));
        contact.EnsureSuccessStatusCode();

        var category = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/categories",
            new NamedItemRequest("Cafe"));
        category.EnsureSuccessStatusCode();

        var service = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/services",
            new NamedItemRequest("Pour-over", "Single origin"));
        service.EnsureSuccessStatusCode();

        var brand = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/brands",
            new NamedItemRequest("Harbour Blend"));
        brand.EnsureSuccessStatusCode();

        var approved = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/facts",
            new FactRequest("CLAIM", "House roasted weekly", "Approved"));
        approved.EnsureSuccessStatusCode();
        var restricted = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/facts",
            new FactRequest("GSTIN", "27AAAAA0000A1Z5", "Restricted"));
        var restrictedBody = await restricted.Content.ReadFromJsonAsync<FactResponse>();
        Assert.False(restrictedBody!.CanPublish);

        var customer = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/customers",
            new CustomerRequest("Ananya", "9876543210", "ananya@example.com", null));
        customer.EnsureSuccessStatusCode();

        var identity = await _client.GetFromJsonAsync<BusinessIdentityResponse>($"/v1/businesses/{session.BusinessId}/identity");
        Assert.Equal("Ledger Cafe", identity!.Business.Name);
        Assert.Equal("HOSPITALITY", identity.Business.IndustryCode);
        Assert.Contains(identity.Contacts, c => c.Kind == "Phone");
        Assert.Contains(identity.Categories, c => c.Name == "Cafe");
        Assert.Contains(identity.Services, s => s.Name == "Pour-over");
        Assert.Contains(identity.Brands, b => b.Name == "Harbour Blend");
        Assert.Contains(identity.Facts, f => f.CanPublish);
        Assert.Contains(identity.Facts, f => !f.CanPublish && f.FactTypeCode == "GSTIN");
        Assert.Contains(identity.Customers, c => c.DisplayName == "Ananya" && c.Contacts.Any(x => x.Kind == "Mobile"));
    }

    [Fact]
    public async Task Identity_stays_isolated_across_tenants()
    {
        var userA = await RegisterAndOnboard("Direct", "Alpha Identity");
        var userB = await RegisterAndOnboard("Direct", "Beta Identity");

        UseToken(userA.Token);
        await _client.PostAsJsonAsync(
            $"/v1/businesses/{userA.BusinessId}/facts",
            new FactRequest("CLAIM", "Only Alpha may see this", "Approved"));

        UseToken(userB.Token);
        var peek = await _client.GetAsync($"/v1/businesses/{userA.BusinessId}/identity");
        Assert.Equal(HttpStatusCode.NotFound, peek.StatusCode);

        var steal = await _client.PostAsJsonAsync(
            $"/v1/businesses/{userA.BusinessId}/customers",
            new CustomerRequest("Intruder", "9000000000", null, null));
        Assert.Equal(HttpStatusCode.NotFound, steal.StatusCode);
    }

    [Fact]
    public async Task Connection_center_connects_assisted_and_oauth_without_inventing_live_apis()
    {
        var session = await RegisterAndOnboard("Direct", "Connect Co");
        UseToken(session.Token);
        var plan = await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        plan.EnsureSuccessStatusCode();

        var center = await _client.GetFromJsonAsync<ConnectionCenterResponse>($"/v1/businesses/{session.BusinessId}/connections");
        Assert.Contains(center!.Catalog, p => p.Code == "GOOGLE");
        Assert.Contains(center.Catalog, p => p.Code == "INDIAMART" && p.Capabilities.AssistedOnly);
        Assert.Contains(center.Catalog, p => p.Code == "WHATSAPP");

        var assisted = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/connections",
            new StartConnectionRequest("INDIAMART"));
        assisted.EnsureSuccessStatusCode();
        var assistedBody = await assisted.Content.ReadFromJsonAsync<StartConnectionResponse>();
        Assert.True(assistedBody!.CompleteInPlace);
        Assert.Equal("Connected", assistedBody.Connection.Status);
        Assert.Equal("Development", assistedBody.Connection.GrantKind);

        var oauth = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/connections",
            new StartConnectionRequest("GOOGLE"));
        oauth.EnsureSuccessStatusCode();
        var oauthBody = await oauth.Content.ReadFromJsonAsync<StartConnectionResponse>();
        Assert.False(oauthBody!.CompleteInPlace);
        Assert.Equal("Connecting", oauthBody.Connection.Status);

        var completed = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/connections/{oauthBody.Connection.Id}/complete",
            new CompleteConnectionRequest("development"));
        completed.EnsureSuccessStatusCode();
        var google = await completed.Content.ReadFromJsonAsync<ConnectionResponse>();
        Assert.Equal("Connected", google!.Status);

        var health = await _client.PostAsync(
            $"/v1/businesses/{session.BusinessId}/connections/{google.Id}/health", null);
        health.EnsureSuccessStatusCode();

        var diagnostics = await _client.PostAsync(
            $"/v1/businesses/{session.BusinessId}/connections/{google.Id}/diagnose", null);
        var checks = await diagnostics.Content.ReadFromJsonAsync<List<DiagnosticResponse>>();
        Assert.Contains(checks!, c => c.Check == "Live provider API" && c.Status == "Hold");
    }

    [Fact]
    public async Task Connections_stay_isolated_across_tenants()
    {
        var userA = await RegisterAndOnboard("Direct", "Alpha Link");
        UseToken(userA.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        await _client.PostAsJsonAsync($"/v1/businesses/{userA.BusinessId}/connections", new StartConnectionRequest("WEBSITE"));

        var userB = await RegisterAndOnboard("Direct", "Beta Link");
        UseToken(userB.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        var peek = await _client.GetAsync($"/v1/businesses/{userA.BusinessId}/connections");
        Assert.Equal(HttpStatusCode.NotFound, peek.StatusCode);
        var steal = await _client.PostAsJsonAsync(
            $"/v1/businesses/{userA.BusinessId}/connections",
            new StartConnectionRequest("GOOGLE"));
        Assert.Equal(HttpStatusCode.NotFound, steal.StatusCode);
    }

    [Fact]
    public async Task DigitalPulse_check_stores_identity_and_connection_findings_without_invented_listings()
    {
        var session = await RegisterAndOnboard("Direct", "Check Co");
        UseToken(session.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));

        var first = await _client.PostAsync($"/v1/businesses/{session.BusinessId}/scans", null);
        first.EnsureSuccessStatusCode();
        var thin = await first.Content.ReadFromJsonAsync<ScanDetailResponse>();
        Assert.Equal("Completed", thin!.Status);
        Assert.Contains(thin.Findings, f => f.Title.Contains("website", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(thin.Findings, f => f.Title.Contains("Phone", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(thin.Findings, f => f.Title.Contains("platforms", StringComparison.OrdinalIgnoreCase));
        Assert.All(thin.Findings, f => Assert.NotEmpty(f.Evidence));

        await _client.PutAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/profile",
            new UpdateBusinessProfileRequest("Ledger Cafe", "https://ledger.example", 2014, "Warm.", "HOSPITALITY"));
        await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/contacts",
            new ContactPointRequest("Phone", "+91 22 1234 5678", "Front desk"));
        await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/categories",
            new NamedItemRequest("Cafe"));
        await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/services",
            new NamedItemRequest("Pour-over"));
        await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/facts",
            new FactRequest("GSTIN", "27AAAAA0000A1Z5", "Restricted"));
        await _client.PostAsJsonAsync($"/v1/businesses/{session.BusinessId}/connections", new StartConnectionRequest("INDIAMART"));

        var second = await _client.PostAsync($"/v1/businesses/{session.BusinessId}/scans", null);
        second.EnsureSuccessStatusCode();
        var rich = await second.Content.ReadFromJsonAsync<ScanDetailResponse>();
        Assert.Contains(rich!.Findings, f => f.Category == "Policy" && f.ObservedValue!.Contains("GSTIN"));
        Assert.Contains(rich.Findings, f => f.Title.Contains("snapshot is unavailable", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(rich.Findings, f => f.Description.Contains("4.8", StringComparison.Ordinal));
        Assert.DoesNotContain(rich.Findings, f => f.Title.Contains("Google reviews", StringComparison.OrdinalIgnoreCase));

        var acknowledged = rich.Findings.First();
        var patch = await _client.PatchAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/findings/{acknowledged.Id}",
            new UpdateFindingRequest("Acknowledged"));
        patch.EnsureSuccessStatusCode();

        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/v1/dashboard");
        Assert.NotNull(dashboard!.LastScanAtUtc);
        Assert.True(dashboard.OpenFindingCount >= 0);

        var third = await _client.PostAsync($"/v1/businesses/{session.BusinessId}/scans", null);
        Assert.Equal(HttpStatusCode.BadRequest, third.StatusCode);
    }

    [Fact]
    public async Task Scans_stay_isolated_across_tenants()
    {
        var userA = await RegisterAndOnboard("Direct", "Alpha Scan");
        UseToken(userA.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        var created = await _client.PostAsync($"/v1/businesses/{userA.BusinessId}/scans", null);
        created.EnsureSuccessStatusCode();
        var scan = await created.Content.ReadFromJsonAsync<ScanDetailResponse>();

        var userB = await RegisterAndOnboard("Direct", "Beta Scan");
        UseToken(userB.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        var peek = await _client.GetAsync($"/v1/businesses/{userA.BusinessId}/scans");
        Assert.Equal(HttpStatusCode.NotFound, peek.StatusCode);
        var steal = await _client.PostAsync($"/v1/businesses/{userA.BusinessId}/scans", null);
        Assert.Equal(HttpStatusCode.NotFound, steal.StatusCode);
        var detail = await _client.GetAsync($"/v1/businesses/{userA.BusinessId}/scans/{scan!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
    }

    [Fact]
    public async Task Website_analysis_stores_on_page_signals_without_invented_search_console()
    {
        var session = await RegisterAndOnboard("Direct", "Web Co");
        UseToken(session.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));

        var missing = await _client.PostAsync($"/v1/businesses/{session.BusinessId}/website/analyze", null);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

        await _client.PutAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/profile",
            new UpdateBusinessProfileRequest("Harbour Coffee", "https://harbour.example", null, null, null));

        var analyzed = await _client.PostAsync($"/v1/businesses/{session.BusinessId}/website/analyze", null);
        analyzed.EnsureSuccessStatusCode();
        var body = await analyzed.Content.ReadFromJsonAsync<WebsiteIntelligenceResponse>();
        Assert.Equal("Reached", body!.Snapshot!.Status);
        Assert.Equal("InMemory", body.SearchProvider);
        Assert.False(body.VectorSearchConfigured);
        Assert.Contains(body.Observations, o => o.Category == "Seo");
        Assert.Contains(body.Observations, o => o.Category == "Aeo");
        Assert.Contains(body.Observations, o => o.Title.Contains("Search Console is not connected", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(body.Observations, o => o.Title.Contains("clicks", StringComparison.OrdinalIgnoreCase));

        var gscStart = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/connections",
            new StartConnectionRequest("SEARCH_CONSOLE"));
        var gscBodyStart = await gscStart.Content.ReadFromJsonAsync<StartConnectionResponse>();
        await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/connections/{gscBodyStart!.Connection.Id}/complete",
            new CompleteConnectionRequest("development"));
        var withGsc = await _client.PostAsync($"/v1/businesses/{session.BusinessId}/website/analyze", null);
        var gscBody = await withGsc.Content.ReadFromJsonAsync<WebsiteIntelligenceResponse>();
        Assert.Contains(gscBody!.Observations, o => o.Title.Contains("Search Console snapshot is unavailable", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Hold", gscBody.SearchConsole.Status);

        var search = await _client.GetFromJsonAsync<SiteSearchResponse>($"/v1/businesses/{session.BusinessId}/website/search?q=Welcome");
        Assert.Contains(search!.Hits, h => h.Snippet.Contains("Welcome", StringComparison.OrdinalIgnoreCase));

        var shortQuery = await _client.GetAsync($"/v1/businesses/{session.BusinessId}/website/search?q=x");
        Assert.Equal(HttpStatusCode.BadRequest, shortQuery.StatusCode);

        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/v1/dashboard");
        Assert.Equal("InMemory", dashboard!.SearchProvider);
        Assert.NotNull(dashboard.LastWebsiteAtUtc);
        Assert.True(dashboard.WebsiteObservationCount > 0);
    }

    [Fact]
    public async Task Website_intelligence_stays_isolated_across_tenants()
    {
        var userA = await RegisterAndOnboard("Direct", "Alpha Web");
        UseToken(userA.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        await _client.PutAsJsonAsync(
            $"/v1/businesses/{userA.BusinessId}/profile",
            new UpdateBusinessProfileRequest("Alpha Cafe", "https://alpha.example", null, null, null));
        await _client.PostAsync($"/v1/businesses/{userA.BusinessId}/website/analyze", null);

        var userB = await RegisterAndOnboard("Direct", "Beta Web");
        UseToken(userB.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        var peek = await _client.GetAsync($"/v1/businesses/{userA.BusinessId}/website");
        Assert.Equal(HttpStatusCode.NotFound, peek.StatusCode);
        var steal = await _client.PostAsync($"/v1/businesses/{userA.BusinessId}/website/analyze", null);
        Assert.Equal(HttpStatusCode.NotFound, steal.StatusCode);
        var search = await _client.GetAsync($"/v1/businesses/{userA.BusinessId}/website/search?q=Welcome");
        Assert.Equal(HttpStatusCode.NotFound, search.StatusCode);
    }

    [Fact]
    public async Task Social_workspace_drafts_and_holds_publish_without_invented_metrics()
    {
        var session = await RegisterAndOnboard("Direct", "Social Co");
        UseToken(session.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));

        var whatsapp = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/social/content",
            new CreateSocialContentRequest("WHATSAPP", "Hi", "Hello"));
        Assert.Equal(HttpStatusCode.BadRequest, whatsapp.StatusCode);

        var created = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/social/content",
            new CreateSocialContentRequest("FACEBOOK", "Weekend hours", "Open until 8 on Saturday."));
        created.EnsureSuccessStatusCode();
        var draft = await created.Content.ReadFromJsonAsync<SocialContentResponse>();
        Assert.Equal("Draft", draft!.Status);
        Assert.Equal("FacebookPost", draft.Kind);

        var earlyPublish = await _client.PostAsync(
            $"/v1/businesses/{session.BusinessId}/social/content/{draft.Id}/publish", null);
        Assert.Equal(HttpStatusCode.BadRequest, earlyPublish.StatusCode);

        await _client.PostAsync($"/v1/businesses/{session.BusinessId}/social/content/{draft.Id}/approve", null);
        var start = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/connections",
            new StartConnectionRequest("FACEBOOK"));
        var startBody = await start.Content.ReadFromJsonAsync<StartConnectionResponse>();
        await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/connections/{startBody!.Connection.Id}/complete",
            new CompleteConnectionRequest("development"));

        var published = await _client.PostAsync(
            $"/v1/businesses/{session.BusinessId}/social/content/{draft.Id}/publish", null);
        published.EnsureSuccessStatusCode();
        var after = await published.Content.ReadFromJsonAsync<SocialContentResponse>();
        Assert.Equal("Blocked", after!.Status);
        Assert.Equal("Hold", after.VerificationStatus);
        Assert.DoesNotContain("Published", after.Status, StringComparison.Ordinal);

        var metrics = await _client.PostAsync($"/v1/businesses/{session.BusinessId}/social/metrics/refresh", null);
        metrics.EnsureSuccessStatusCode();
        var workspace = await metrics.Content.ReadFromJsonAsync<SocialWorkspaceResponse>();
        Assert.DoesNotContain(workspace!.Channels, c => c.PlatformCode == "WHATSAPP");
        Assert.Contains(workspace.Channels, c => c.PlatformCode == "FACEBOOK" && c.MetricStatus == "Hold");
        Assert.DoesNotContain(workspace.Channels, c => c.MetricDetail?.Any(char.IsDigit) == true && c.MetricDetail!.Contains("likes", StringComparison.OrdinalIgnoreCase));

        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/v1/dashboard");
        Assert.Equal(0, dashboard!.SocialDraftCount);
        Assert.True(dashboard.SocialBlockedCount >= 1);
    }

    [Fact]
    public async Task Social_content_stays_isolated_across_tenants()
    {
        var userA = await RegisterAndOnboard("Direct", "Alpha Social");
        UseToken(userA.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        var created = await _client.PostAsJsonAsync(
            $"/v1/businesses/{userA.BusinessId}/social/content",
            new CreateSocialContentRequest("LINKEDIN", "Only A", "Alpha only"));
        var draft = await created.Content.ReadFromJsonAsync<SocialContentResponse>();

        var userB = await RegisterAndOnboard("Direct", "Beta Social");
        UseToken(userB.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        var peek = await _client.GetAsync($"/v1/businesses/{userA.BusinessId}/social");
        Assert.Equal(HttpStatusCode.NotFound, peek.StatusCode);
        var steal = await _client.PostAsJsonAsync(
            $"/v1/businesses/{userA.BusinessId}/social/content",
            new CreateSocialContentRequest("LINKEDIN", "Intruder", "No"));
        Assert.Equal(HttpStatusCode.NotFound, steal.StatusCode);
        var approve = await _client.PostAsync($"/v1/businesses/{userA.BusinessId}/social/content/{draft!.Id}/approve", null);
        Assert.Equal(HttpStatusCode.NotFound, approve.StatusCode);
    }

    [Fact]
    public async Task Directory_playbooks_are_assisted_and_never_scrape()
    {
        var session = await RegisterAndOnboard("Direct", "Directory Co");
        UseToken(session.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        await _client.PutAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/profile",
            new UpdateBusinessProfileRequest("Harbour Coffee", "https://harbour.example", null, null, null));

        var social = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/directories/prepare",
            new PrepareDirectoryRequest("FACEBOOK"));
        Assert.Equal(HttpStatusCode.BadRequest, social.StatusCode);

        var missing = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/directories/prepare",
            new PrepareDirectoryRequest("INDIAMART"));
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

        var enabled = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/connections",
            new StartConnectionRequest("INDIAMART"));
        enabled.EnsureSuccessStatusCode();

        var prepared = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/directories/prepare",
            new PrepareDirectoryRequest("INDIAMART"));
        prepared.EnsureSuccessStatusCode();
        var task = await prepared.Content.ReadFromJsonAsync<DirectoryTaskResponse>();
        Assert.Equal("Harbour Coffee", task!.PreparedName);
        Assert.Equal("https://harbour.example", task.PreparedWebsite);
        Assert.True(task.Steps.Count >= 4);

        var early = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/directories/tasks/{task.Id}/verify",
            new VerifyDirectoryRequest("Too soon"));
        Assert.Equal(HttpStatusCode.BadRequest, early.StatusCode);

        foreach (var step in task.Steps)
        {
            var done = await _client.PostAsync(
                $"/v1/businesses/{session.BusinessId}/directories/tasks/{task.Id}/steps/{step.Id}/complete", null);
            done.EnsureSuccessStatusCode();
        }

        var verified = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/directories/tasks/{task.Id}/verify",
            new VerifyDirectoryRequest("Confirmed on the official seller profile."));
        verified.EnsureSuccessStatusCode();
        var after = await verified.Content.ReadFromJsonAsync<DirectoryTaskResponse>();
        Assert.Equal("Verified", after!.Status);

        var workspace = await _client.GetFromJsonAsync<DirectoryWorkspaceResponse>($"/v1/businesses/{session.BusinessId}/directories");
        Assert.Contains(workspace!.Providers, p => p.Capabilities.PlatformCode == "INDIAMART" && p.ReadStatus == "Hold");
        Assert.Contains(workspace.Providers, p => p.Capabilities.PlatformCode == "JUSTDIAL" && !p.Capabilities.CanWriteOfficially);
        Assert.DoesNotContain(workspace.Providers, p => p.Capabilities.PlatformCode == "FACEBOOK");

        var monitor = await _client.PostAsync($"/v1/businesses/{session.BusinessId}/directories/INDIAMART/monitor", null);
        monitor.EnsureSuccessStatusCode();
        var monitored = await monitor.Content.ReadFromJsonAsync<DirectoryWorkspaceResponse>();
        Assert.Contains(monitored!.Tasks, t => t.MonitorDetail != null && t.MonitorDetail.Contains("hold", StringComparison.OrdinalIgnoreCase));

        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/v1/dashboard");
        Assert.True(dashboard!.DirectoryVerifiedCount >= 1);
    }

    [Fact]
    public async Task Directory_tasks_stay_isolated_across_tenants()
    {
        var userA = await RegisterAndOnboard("Direct", "Alpha Dir");
        UseToken(userA.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        await _client.PostAsJsonAsync($"/v1/businesses/{userA.BusinessId}/connections", new StartConnectionRequest("JUSTDIAL"));
        await _client.PostAsJsonAsync(
            $"/v1/businesses/{userA.BusinessId}/directories/prepare",
            new PrepareDirectoryRequest("JUSTDIAL"));

        var userB = await RegisterAndOnboard("Direct", "Beta Dir");
        UseToken(userB.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        var peek = await _client.GetAsync($"/v1/businesses/{userA.BusinessId}/directories");
        Assert.Equal(HttpStatusCode.NotFound, peek.StatusCode);
        var steal = await _client.PostAsJsonAsync(
            $"/v1/businesses/{userA.BusinessId}/directories/prepare",
            new PrepareDirectoryRequest("JUSTDIAL"));
        Assert.Equal(HttpStatusCode.NotFound, steal.StatusCode);
    }

    [Fact]
    public async Task Project_factory_approves_only_inside_permission_scope()
    {
        var session = await RegisterAndOnboard("Direct", "Project Co");
        UseToken(session.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));

        var service = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/services",
            new NamedItemRequest("Roasting"));
        service.EnsureSuccessStatusCode();
        var serviceBody = await service.Content.ReadFromJsonAsync<NamedItemResponse>();

        var created = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/projects",
            new CreateProjectRequest("Harbour Roast", "Harbour Cafe", "FOOD", "Mumbai", "A flagship roast program.", "Repeat wholesale.", null, null, "Partial", "Internal"));
        created.EnsureSuccessStatusCode();
        var detail = await created.Content.ReadFromJsonAsync<ProjectDetailResponse>();
        Assert.Equal("Partial", detail!.Project.PermissionScope);

        await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/projects/{detail.Project.Id}/services",
            new LinkNamedRequest(serviceBody!.Id));
        await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/projects/{detail.Project.Id}/media",
            new RegisterMediaRequest("Hero", "Image", null));

        var factory = await _client.PostAsync(
            $"/v1/businesses/{session.BusinessId}/projects/{detail.Project.Id}/factory", null);
        factory.EnsureSuccessStatusCode();
        var pack = await factory.Content.ReadFromJsonAsync<ProjectDetailResponse>();
        Assert.Single(pack!.Packs);
        Assert.Equal(12, pack.Packs[0].Variants.Count);
        Assert.Contains(pack.Packs[0].Variants, v => v.Kind == "WhatsAppTemplateDraft");

        var request = await _client.PostAsync(
            $"/v1/businesses/{session.BusinessId}/projects/{detail.Project.Id}/content/{pack.Packs[0].Id}/approvals", null);
        request.EnsureSuccessStatusCode();
        var pending = await request.Content.ReadFromJsonAsync<ProjectDetailResponse>();
        var approval = pending!.Packs[0].Approvals.Single(a => a.Open);

        var decided = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/projects/{detail.Project.Id}/approvals/{approval.Id}/decide",
            new DecideApprovalRequest(true, "Approved against stored permission scope."));
        decided.EnsureSuccessStatusCode();
        var after = await decided.Content.ReadFromJsonAsync<ProjectDetailResponse>();
        Assert.Contains(after!.Packs[0].Variants, v => v.Kind == "WebsiteCaseStudy" && v.Status == "Approved");
        Assert.Contains(after.Packs[0].Variants, v => v.Kind == "LinkedInPost" && v.Status == "Hold");
        Assert.All(after.Packs[0].Variants, v => Assert.DoesNotContain("Published", v.Status, StringComparison.Ordinal));

        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/v1/dashboard");
        Assert.True(dashboard!.ProjectCount >= 1);
        Assert.True(dashboard.ContentHoldCount >= 1);
    }

    [Fact]
    public async Task Projects_stay_isolated_across_tenants()
    {
        var userA = await RegisterAndOnboard("Direct", "Alpha Projects");
        UseToken(userA.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        var created = await _client.PostAsJsonAsync(
            $"/v1/businesses/{userA.BusinessId}/projects",
            new CreateProjectRequest("Secret", null, null, null, null, null, null, null, "None", "Restricted"));
        var body = await created.Content.ReadFromJsonAsync<ProjectDetailResponse>();

        var userB = await RegisterAndOnboard("Direct", "Beta Projects");
        UseToken(userB.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        var peek = await _client.GetAsync($"/v1/businesses/{userA.BusinessId}/projects");
        Assert.Equal(HttpStatusCode.NotFound, peek.StatusCode);
        var steal = await _client.GetAsync($"/v1/businesses/{userA.BusinessId}/projects/{body!.Project.Id}");
        Assert.Equal(HttpStatusCode.NotFound, steal.StatusCode);
        var factory = await _client.PostAsync($"/v1/businesses/{userA.BusinessId}/projects/{body.Project.Id}/factory", null);
        Assert.Equal(HttpStatusCode.NotFound, factory.StatusCode);
    }

    [Fact]
    public async Task Ai_orchestrator_holds_without_a_live_provider_and_rejects_no_evidence()
    {
        var session = await RegisterAndOnboard("Direct", "Orchestrator Co");
        UseToken(session.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));

        var empty = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/ai/runs",
            new RunAiRequest("research", "What should we publish this week?"));
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        var rejected = await empty.Content.ReadFromJsonAsync<AiRunResponse>();
        Assert.Equal("Rejected", rejected!.Status);
        Assert.False(rejected.ProviderIsLive);
        Assert.Contains("No evidence", rejected.HoldReason, StringComparison.OrdinalIgnoreCase);

        var fact = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/facts",
            new FactRequest("CLAIM", "Harbour Roast", "Approved"));
        fact.EnsureSuccessStatusCode();
        var knowledge = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/ai/knowledge",
            new AddKnowledgeRequest("Brand voice", "Warm and precise. Never invent reviews.", "Note", null));
        knowledge.EnsureSuccessStatusCode();

        var restricted = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/facts",
            new FactRequest("GSTIN", "27AAAAA0000A1Z5", "Restricted"));
        restricted.EnsureSuccessStatusCode();

        var workspace = await _client.GetFromJsonAsync<AiWorkspaceResponse>($"/v1/businesses/{session.BusinessId}/ai");
        Assert.False(workspace!.ProviderIsLive);
        Assert.Contains(workspace.Agents, a => a.Code == "content");
        Assert.Contains(workspace.Knowledge, k => k.Title == "Brand voice");
        Assert.Contains(workspace.Nodes, n => n.Kind == "Business");

        var run = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/ai/runs",
            new RunAiRequest("identity", "Restate the canonical identity for Harbour Roast."));
        run.EnsureSuccessStatusCode();
        var held = await run.Content.ReadFromJsonAsync<AiRunResponse>();
        Assert.Equal("Held", held!.Status);
        Assert.Equal("Development", held.ProviderName);
        Assert.False(held.ProviderIsLive);
        Assert.Contains("Harbour Roast", held.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("27AAAAA0000A1Z5", held.Output, StringComparison.Ordinal);
        Assert.NotNull(held.Evaluation);
        Assert.True(held.Evaluation!.HasEvidence);
        Assert.Contains(held.Audit, a => a.Stage == "validation");
        Assert.Contains(held.Audit, a => a.Stage == "graphify");

        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/v1/dashboard");
        Assert.True(dashboard!.AiRunCount >= 2);
        Assert.True(dashboard.AiHeldCount >= 1);
    }

    [Fact]
    public async Task Ai_workspace_stays_isolated_across_tenants()
    {
        var userA = await RegisterAndOnboard("Direct", "Alpha Orchestrator");
        UseToken(userA.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        await _client.PostAsJsonAsync(
            $"/v1/businesses/{userA.BusinessId}/ai/knowledge",
            new AddKnowledgeRequest("Secret brief", "Only tenant A may see this.", "Note", null));

        var userB = await RegisterAndOnboard("Direct", "Beta Orchestrator");
        UseToken(userB.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        var peek = await _client.GetAsync($"/v1/businesses/{userA.BusinessId}/ai");
        Assert.Equal(HttpStatusCode.NotFound, peek.StatusCode);
        var steal = await _client.PostAsJsonAsync(
            $"/v1/businesses/{userA.BusinessId}/ai/runs",
            new RunAiRequest("research", "Steal the other tenant brief."));
        Assert.Equal(HttpStatusCode.NotFound, steal.StatusCode);
    }

    [Fact]
    public async Task Action_engine_requires_approval_holds_live_writes_and_is_idempotent()
    {
        var session = await RegisterAndOnboard("Direct", "Action Co");
        UseToken(session.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));

        var workspace = await _client.GetFromJsonAsync<ActionWorkspaceResponse>($"/v1/businesses/{session.BusinessId}/actions");
        Assert.Equal("Assisted", workspace!.Policy.Mode);
        Assert.Equal(25, workspace.ActionsPerMonth);
        Assert.Contains(workspace.Kinds, k => k.Code == "publish-social" && k.ExternalWrite);
        Assert.Contains("never invents", workspace.Note, StringComparison.OrdinalIgnoreCase);

        var first = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/actions",
            new EnqueueActionRequest("rebuild-graphify", "Rebuild Graphify", null, null));
        first.EnsureSuccessStatusCode();
        var pending = await first.Content.ReadFromJsonAsync<WorkActionResponse>();
        Assert.Equal("PendingApproval", pending!.Status);
        Assert.False(pending.AutopilotEligible);

        var again = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/actions",
            new EnqueueActionRequest("rebuild-graphify", "Rebuild Graphify again", null, null));
        again.EnsureSuccessStatusCode();
        var same = await again.Content.ReadFromJsonAsync<WorkActionResponse>();
        Assert.Equal(pending.Id, same!.Id);

        var publish = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/actions",
            new EnqueueActionRequest("publish-social", "Publish a held post", null, null));
        publish.EnsureSuccessStatusCode();
        var heldWrite = await publish.Content.ReadFromJsonAsync<WorkActionResponse>();
        Assert.Equal("PendingApproval", heldWrite!.Status);
        Assert.False(heldWrite.AutopilotEligible);
        Assert.False(heldWrite.LiveWriteAvailable);
        Assert.Contains("will not invent", heldWrite.HoldReason, StringComparison.OrdinalIgnoreCase);

        var approvedWrite = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/actions/{heldWrite.Id}/approve",
            new { });
        approvedWrite.EnsureSuccessStatusCode();
        var executedWrite = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/actions/{heldWrite.Id}/execute",
            new { });
        executedWrite.EnsureSuccessStatusCode();
        var assisted = await executedWrite.Content.ReadFromJsonAsync<WorkActionResponse>();
        Assert.Equal("Assisted", assisted!.Status);
        Assert.NotEmpty(assisted.Attempts);
        Assert.Contains("draft", assisted.HoldReason, StringComparison.OrdinalIgnoreCase);

        var policy = await _client.PutAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/actions/policy",
            new UpdateAutomationPolicyRequest("FullAuto", true, 3));
        policy.EnsureSuccessStatusCode();

        var approved = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/actions/{pending.Id}/approve",
            new { });
        approved.EnsureSuccessStatusCode();
        var executed = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/actions/{pending.Id}/execute",
            new { });
        executed.EnsureSuccessStatusCode();
        var rebuilt = await executed.Content.ReadFromJsonAsync<WorkActionResponse>();
        Assert.Equal("Executed", rebuilt!.Status);
        Assert.Contains("Graphify", rebuilt.HoldReason, StringComparison.OrdinalIgnoreCase);

        var verified = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/actions/{pending.Id}/verify",
            new { });
        verified.EnsureSuccessStatusCode();
        var done = await verified.Content.ReadFromJsonAsync<WorkActionResponse>();
        Assert.Equal("Verified", done!.Status);

        var autopilot = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/actions",
            new EnqueueActionRequest("monitor-directory", "Watch IndiaMART", null, "INDIAMART"));
        autopilot.EnsureSuccessStatusCode();
        var queued = await autopilot.Content.ReadFromJsonAsync<WorkActionResponse>();
        Assert.True(queued!.Status is "Assisted" or "Executed" or "Queued");
        Assert.True(queued.AutopilotEligible);
        Assert.Contains("invent", queued.HoldReason, StringComparison.OrdinalIgnoreCase);

        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/v1/dashboard");
        Assert.True(dashboard!.ActionOpenCount >= 1);
        Assert.True(dashboard.ActionHeldCount >= 1);
    }

    [Fact]
    public async Task Actions_stay_isolated_across_tenants()
    {
        var userA = await RegisterAndOnboard("Direct", "Alpha Actions");
        UseToken(userA.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        var created = await _client.PostAsJsonAsync(
            $"/v1/businesses/{userA.BusinessId}/actions",
            new EnqueueActionRequest("rebuild-graphify", "Secret rebuild", null, null));
        var body = await created.Content.ReadFromJsonAsync<WorkActionResponse>();

        var userB = await RegisterAndOnboard("Direct", "Beta Actions");
        UseToken(userB.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        var peek = await _client.GetAsync($"/v1/businesses/{userA.BusinessId}/actions");
        Assert.Equal(HttpStatusCode.NotFound, peek.StatusCode);
        var steal = await _client.PostAsJsonAsync(
            $"/v1/businesses/{userA.BusinessId}/actions/{body!.Id}/approve",
            new { });
        Assert.Equal(HttpStatusCode.NotFound, steal.StatusCode);
        var execute = await _client.PostAsJsonAsync(
            $"/v1/businesses/{userA.BusinessId}/actions/{body.Id}/execute",
            new { });
        Assert.Equal(HttpStatusCode.NotFound, execute.StatusCode);
    }

    [Fact]
    public async Task Starter_plan_cannot_connect_whatsapp()
    {
        var session = await RegisterAndOnboard("Direct", "Starter WhatsApp");
        UseToken(session.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        var connect = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/whatsapp/connect",
            new ConnectWhatsAppRequest("Starter Cafe", "+912200000000"));
        Assert.Equal(HttpStatusCode.BadRequest, connect.StatusCode);
    }

    [Fact]
    public async Task WhatsApp_holds_sends_without_cloud_api_and_enforces_consent()
    {
        var session = await RegisterAndOnboard("Direct", "WhatsApp Co");
        UseToken(session.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("GROWTH"));
        await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/customers",
            new CustomerRequest("Priya Shah", "+912211110001", null, null));

        var connected = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/whatsapp/connect",
            new ConnectWhatsAppRequest("Harbour Roast", "+912200000111"));
        connected.EnsureSuccessStatusCode();
        var workspace = await connected.Content.ReadFromJsonAsync<WhatsAppWorkspaceResponse>();
        Assert.True(workspace!.PlanEnabled);
        Assert.False(workspace.Account!.CloudApiIsLive);
        Assert.Equal(2000, workspace.MessagesPerMonth);

        var imported = await _client.PostAsync($"/v1/businesses/{session.BusinessId}/whatsapp/contacts/import", null);
        imported.EnsureSuccessStatusCode();
        workspace = await imported.Content.ReadFromJsonAsync<WhatsAppWorkspaceResponse>();
        var contact = Assert.Single(workspace!.Contacts);
        Assert.Equal("Unknown", contact.Consent);

        var template = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/whatsapp/templates",
            new CreateWhatsAppTemplateRequest("utility_update", "en", "UTILITY", "Your order is ready at the counter."));
        template.EnsureSuccessStatusCode();
        var saved = await template.Content.ReadFromJsonAsync<WhatsAppTemplateResponse>();
        await _client.PostAsJsonAsync($"/v1/businesses/{session.BusinessId}/whatsapp/templates/{saved!.Id}/approve", new { });

        var blocked = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/whatsapp/messages",
            new DraftWhatsAppMessageRequest(contact.Id, "Template", "Your order is ready at the counter.", saved.Id));
        blocked.EnsureSuccessStatusCode();
        var draft = await blocked.Content.ReadFromJsonAsync<WhatsAppMessageResponse>();
        await _client.PostAsJsonAsync($"/v1/businesses/{session.BusinessId}/whatsapp/messages/{draft!.Id}/approve", new { });
        var sendBlocked = await _client.PostAsJsonAsync($"/v1/businesses/{session.BusinessId}/whatsapp/messages/{draft.Id}/send", new { });
        sendBlocked.EnsureSuccessStatusCode();
        var failed = await sendBlocked.Content.ReadFromJsonAsync<WhatsAppMessageResponse>();
        Assert.Equal("Failed", failed!.Status);
        Assert.Contains("opt-in", failed.HoldReason, StringComparison.OrdinalIgnoreCase);

        await _client.PostAsync($"/v1/businesses/{session.BusinessId}/whatsapp/contacts/{contact.Id}/opt-in", null);
        var sendHeld = await _client.PostAsJsonAsync($"/v1/businesses/{session.BusinessId}/whatsapp/messages/{draft.Id}/send", new { });
        sendHeld.EnsureSuccessStatusCode();
        var held = await sendHeld.Content.ReadFromJsonAsync<WhatsAppMessageResponse>();
        Assert.Equal("Held", held!.Status);
        Assert.Contains("not configured", held.HoldReason, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(held.Attempts);

        var inbound = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/whatsapp/inbound",
            new RecordWhatsAppInboundRequest(contact.Id, "Can I collect after 6?"));
        inbound.EnsureSuccessStatusCode();
        var afterInbound = await inbound.Content.ReadFromJsonAsync<WhatsAppWorkspaceResponse>();
        Assert.Contains(afterInbound!.Contacts, c => c.WindowOpen);
        Assert.Contains(afterInbound.Messages, m => m.Kind == "Inbound" && m.Untrusted);

        var sessionReply = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/whatsapp/conversations/{afterInbound.Conversations[0].Id}/reply",
            new ReplyWhatsAppRequest("Yes, the counter stays open until 7."));
        sessionReply.EnsureSuccessStatusCode();
        var reply = await sessionReply.Content.ReadFromJsonAsync<WhatsAppMessageResponse>();
        Assert.Equal("Held", reply!.Status);
        Assert.Equal("Session", reply.Kind);

        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/v1/dashboard");
        Assert.True(dashboard!.WhatsAppOptInCount >= 1);
        Assert.True(dashboard.WhatsAppHeldCount >= 1);
    }

    [Fact]
    public async Task WhatsApp_stays_isolated_across_tenants()
    {
        var userA = await RegisterAndOnboard("Direct", "Alpha WhatsApp");
        UseToken(userA.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("GROWTH"));
        await _client.PostAsJsonAsync(
            $"/v1/businesses/{userA.BusinessId}/whatsapp/connect",
            new ConnectWhatsAppRequest("Alpha", "+912200000222"));

        var userB = await RegisterAndOnboard("Direct", "Beta WhatsApp");
        UseToken(userB.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("GROWTH"));
        var peek = await _client.GetAsync($"/v1/businesses/{userA.BusinessId}/whatsapp");
        Assert.Equal(HttpStatusCode.NotFound, peek.StatusCode);
        var steal = await _client.PostAsJsonAsync(
            $"/v1/businesses/{userA.BusinessId}/whatsapp/connect",
            new ConnectWhatsAppRequest("Stolen", "+912200000333"));
        Assert.Equal(HttpStatusCode.NotFound, steal.StatusCode);
    }

    [Fact]
    public async Task Monitoring_records_stored_health_and_holds_live_metrics()
    {
        var session = await RegisterAndOnboard("Direct", "Monitor Co");
        UseToken(session.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("GROWTH"));
        await _client.PutAsJsonAsync($"/v1/businesses/{session.BusinessId}", new UpdateBusinessRequest("Monitor Co Biz", "https://example.com"));

        var opened = await _client.GetAsync($"/v1/businesses/{session.BusinessId}/monitoring");
        opened.EnsureSuccessStatusCode();
        var workspace = await opened.Content.ReadFromJsonAsync<MonitoringWorkspaceResponse>();
        Assert.Equal(24, workspace!.Schedule.IntervalHours);
        Assert.True(workspace.Schedule.Due);
        Assert.Contains(workspace.Kinds, k => k.Code == "search-visibility" && !k.CanObserveWithoutLiveApi);

        var ran = await _client.PostAsync($"/v1/businesses/{session.BusinessId}/monitoring/runs", null);
        ran.EnsureSuccessStatusCode();
        workspace = await ran.Content.ReadFromJsonAsync<MonitoringWorkspaceResponse>();
        var latest = Assert.Single(workspace!.Runs);
        Assert.Equal("Manual", latest.Trigger);
        Assert.Equal("Completed", latest.Status);
        Assert.Contains(latest.Results, r => r.Kind == "search-visibility" && r.Status == "Held");
        Assert.Contains(latest.Results, r => r.Kind == "review-changes" && r.Status == "Held");
        Assert.Contains(latest.Results, r => r.Kind == "website-availability" && r.Status == "Observed");
        Assert.DoesNotContain(latest.Results, r => r.ObservedFact.Contains("invented ranking", StringComparison.OrdinalIgnoreCase));

        var competitor = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/monitoring/competitors",
            new AddCompetitorRequest("Harbor Beans", "https://harbor.example", null));
        competitor.EnsureSuccessStatusCode();

        var report = await _client.PostAsync($"/v1/businesses/{session.BusinessId}/monitoring/reports", null);
        report.EnsureSuccessStatusCode();
        workspace = await report.Content.ReadFromJsonAsync<MonitoringWorkspaceResponse>();
        var pulse = Assert.Single(workspace!.Reports);
        Assert.Equal("Pulse", pulse.Kind);
        Assert.Contains("No AI interpretation", pulse.AiInterpretation, StringComparison.Ordinal);
        Assert.Null(pulse.CustomerDecision);

        var decided = await _client.PostAsJsonAsync(
            $"/v1/businesses/{session.BusinessId}/monitoring/reports/{pulse.Id}/decision",
            new RecordReportDecisionRequest("Accepted. We will review listings next week."));
        decided.EnsureSuccessStatusCode();
        workspace = await decided.Content.ReadFromJsonAsync<MonitoringWorkspaceResponse>();
        Assert.Equal("Accepted. We will review listings next week.", workspace!.Reports[0].CustomerDecision);

        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/v1/dashboard");
        Assert.NotNull(dashboard!.LastMonitoringAtUtc);
        Assert.Equal(24, dashboard.MonitoringIntervalHours);
        Assert.True(dashboard.ReportCount >= 1);
    }

    [Fact]
    public async Task Monitoring_stays_isolated_across_tenants()
    {
        var userA = await RegisterAndOnboard("Direct", "Alpha Monitor");
        UseToken(userA.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        await _client.PostAsync($"/v1/businesses/{userA.BusinessId}/monitoring/runs", null);

        var userB = await RegisterAndOnboard("Direct", "Beta Monitor");
        UseToken(userB.Token);
        await _client.PostAsJsonAsync("/v1/subscriptions", new SelectPlanRequest("STARTER"));
        var peek = await _client.GetAsync($"/v1/businesses/{userA.BusinessId}/monitoring");
        Assert.Equal(HttpStatusCode.NotFound, peek.StatusCode);
        var steal = await _client.PostAsync($"/v1/businesses/{userA.BusinessId}/monitoring/runs", null);
        Assert.Equal(HttpStatusCode.NotFound, steal.StatusCode);
    }

    [Fact]
    public async Task Client_supplied_tenant_header_is_rejected_when_forged()
    {
        var session = await RegisterAndOnboard("Direct", "Forge Co");
        UseToken(session.Token);
        _client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", Guid.NewGuid().ToString());
        var response = await _client.GetAsync("/v1/tenants/current");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<(string Token, Guid BusinessId)> RegisterAndOnboard(string type, string tenantName)
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var register = await _client.PostAsJsonAsync("/v1/auth/register", new RegisterRequest(email, tenantName, "Password1!"));
        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        UseToken(auth!.AccessToken);

        var tenant = await _client.PostAsJsonAsync("/v1/tenants", new CreateTenantRequest(tenantName, type));
        tenant.EnsureSuccessStatusCode();
        var after = await tenant.Content.ReadFromJsonAsync<AuthResponse>();
        UseToken(after!.AccessToken);

        var business = await _client.PostAsJsonAsync("/v1/businesses", new CreateBusinessRequest($"{tenantName} Biz", null));
        business.EnsureSuccessStatusCode();
        var created = await business.Content.ReadFromJsonAsync<BusinessResponse>();
        return (after.AccessToken, created!.Id);
    }

    private void UseToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _client.DefaultRequestHeaders.Remove("X-Tenant-Id");
    }
}
