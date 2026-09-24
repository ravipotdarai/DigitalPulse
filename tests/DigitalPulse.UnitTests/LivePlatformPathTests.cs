using DigitalPulse.Application.Abstractions;
using DigitalPulse.Domain.Actions;
using DigitalPulse.Domain.Platforms;
using DigitalPulse.Infrastructure.Billing;
using DigitalPulse.Infrastructure.Platforms;
using DigitalPulse.Infrastructure.Search;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class LivePlatformPathTests
{
    [Fact]
    public async Task Official_oauth_falls_back_to_development_without_keys()
    {
        var broker = new OfficialOAuthBroker(new UnusedHttpFactory(), EmptyConfig());
        var connection = PlatformConnection.Start(Guid.NewGuid(), Guid.NewGuid(), "GOOGLE", PlatformAuthMode.OAuth, "state-dev");
        var start = await broker.StartAsync(connection, new GoogleAdapter(new FakeGateway()), CancellationToken.None);
        Assert.Contains("code=development", start.AuthorizationUrl, StringComparison.Ordinal);
        Assert.False(start.CompleteInPlace);

        await broker.CompleteAsync(connection, "development", CancellationToken.None);
        Assert.Equal("Development", connection.GrantKind);
        Assert.False(connection.HasLiveCredential);
    }

    [Fact]
    public async Task Official_oauth_uses_provider_authorize_when_keys_exist()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Connections:Google:ClientId"] = "gid.apps.googleusercontent.com",
            ["Connections:Google:ClientSecret"] = "gsecret"
        }).Build();
        var broker = new OfficialOAuthBroker(new UnusedHttpFactory(), config);
        var connection = PlatformConnection.Start(Guid.NewGuid(), Guid.NewGuid(), "SEARCH_CONSOLE", PlatformAuthMode.OAuth, "state-live");
        var start = await broker.StartAsync(connection, new SearchConsoleAdapter(new FakeGateway()), CancellationToken.None);
        Assert.Contains("accounts.google.com", start.AuthorizationUrl, StringComparison.Ordinal);
        Assert.Contains("gid.apps.googleusercontent.com", start.AuthorizationUrl, StringComparison.Ordinal);
        Assert.False(start.CompleteInPlace);
    }

    [Fact]
    public async Task Indiamart_crm_key_attaches_a_live_read_grant()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Connections:IndiaMART:CrmKey"] = "crm-test-key"
        }).Build();
        var broker = new OfficialOAuthBroker(new UnusedHttpFactory(), config);
        var connection = PlatformConnection.Start(Guid.NewGuid(), Guid.NewGuid(), "INDIAMART", PlatformAuthMode.Assisted, "state-im");
        var start = await broker.StartAsync(connection, new IndiaMartAdapter(new FakeGateway()), CancellationToken.None);
        Assert.True(start.CompleteInPlace);
        await broker.CompleteAsync(connection, "development", CancellationToken.None);
        Assert.True(connection.HasLiveCredential);
        Assert.Equal("ApiKey", connection.GrantKind);
    }

    [Fact]
    public async Task Facebook_publish_holds_without_a_live_token()
    {
        var adapter = new FacebookAdapter(new FakeGateway());
        var connection = PlatformConnection.Start(Guid.NewGuid(), Guid.NewGuid(), "FACEBOOK", PlatformAuthMode.OAuth, "state-fb");
        connection.MarkConnected("Development", "dev", "Development · FACEBOOK");
        var result = await adapter.PublishAsync(connection, "Hours", "Open late.", CancellationToken.None);
        Assert.Equal("Hold", result.Status);
        Assert.Contains("will not invent", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Facebook_publish_marks_official_success()
    {
        var adapter = new FacebookAdapter(new FakeGateway());
        var connection = Live("FACEBOOK");
        var result = await adapter.PublishAsync(connection, "Hours", "Open late.", CancellationToken.None);
        Assert.Equal("Published", result.Status);
    }

    [Fact]
    public async Task Justdial_stays_assisted()
    {
        var adapter = new JustdialAdapter();
        var connection = PlatformConnection.Start(Guid.NewGuid(), Guid.NewGuid(), "JUSTDIAL", PlatformAuthMode.Assisted, "state-jd");
        connection.MarkConnected("Assisted", "assisted", "Assisted workspace");
        var publish = await adapter.PublishAsync(connection, "Listing", "Update hours.", CancellationToken.None);
        Assert.Equal("Assisted", publish.Status);
        Assert.False(connection.HasLiveCredential);
    }

    [Fact]
    public async Task Vector_search_is_local_and_tenant_scoped()
    {
        var provider = new LocalHashVectorSearchProvider();
        Assert.True(provider.IsConfigured);
        Assert.Equal("LocalHash", provider.ProviderCode);
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var business = Guid.NewGuid();
        await provider.IndexAsync(new SearchDocument(tenantA, business, "Website", "Harbour", "masala dosa breakfast", "https://a.test/"), CancellationToken.None);
        await provider.IndexAsync(new SearchDocument(tenantB, business, "Website", "Harbour", "masala dosa breakfast", "https://b.test/"), CancellationToken.None);
        var hits = await provider.SearchAsync(tenantA, business, "dosa", CancellationToken.None);
        Assert.Single(hits);
        Assert.Equal("https://a.test/", hits[0].Url);
    }

    [Fact]
    public async Task Razorpay_order_does_not_mark_a_capture()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Billing:Razorpay:KeyId"] = "rzp_test_key",
            ["Billing:Razorpay:KeySecret"] = "rzp_test_secret"
        }).Build();
        var gateway = new RazorpayBillingGateway(new OrderHttpFactory(), config);
        var result = await gateway.CheckoutAsync("GROWTH", 1999m, "INV-1", CancellationToken.None);
        Assert.Equal("OrderCreated", result.Status);
        Assert.Equal("order_test_1", result.Reference);
        Assert.True(result.IsLive);
        Assert.DoesNotContain("Succeeded", result.Status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unpaid", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Autopilot_still_refuses_external_writes_without_a_live_grant()
    {
        var decision = ActionPolicy.Evaluate(ActionKind.PublishSocial, AutomationMode.FullAuto, liveWriteAvailable: false, alreadyApproved: true);
        Assert.False(decision.EligibleForAutopilot);
    }

    private static PlatformConnection Live(string platform)
    {
        var connection = PlatformConnection.Start(Guid.NewGuid(), Guid.NewGuid(), platform, PlatformAuthMode.OAuth, "state-live");
        connection.AttachLiveGrant("OAuth", "oauth-test", platform, "access-token", null, null, "test");
        return connection;
    }

    private static IConfiguration EmptyConfig() => new ConfigurationBuilder().AddInMemoryCollection().Build();

    private sealed class FakeGateway : IOfficialPlatformGateway
    {
        public Task<OfficialHttpResult> SendAsync(
            HttpMethod method,
            string url,
            string? accessToken,
            string? jsonBody,
            IReadOnlyDictionary<string, string>? headers,
            CancellationToken cancellationToken) =>
            Task.FromResult(new OfficialHttpResult(200, """{"id":"p1"}""", true));
    }

    private sealed class UnusedHttpFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private sealed class OrderHttpFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(new OrderHandler(), disposeHandler: false);
    }

    private sealed class OrderHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"order_test_1","amount":199900,"currency":"INR"}""")
            });
    }
}
