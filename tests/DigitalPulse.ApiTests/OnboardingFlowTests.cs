using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DigitalPulse.Contracts.Auth;
using DigitalPulse.Contracts.Billing;
using DigitalPulse.Contracts.Businesses;
using DigitalPulse.Contracts.Connections;
using DigitalPulse.Contracts.Onboarding;
using DigitalPulse.Contracts.Scans;
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
