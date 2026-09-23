using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DigitalPulse.Contracts.Auth;
using DigitalPulse.Contracts.Billing;
using DigitalPulse.Contracts.Businesses;
using DigitalPulse.Contracts.Onboarding;
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
