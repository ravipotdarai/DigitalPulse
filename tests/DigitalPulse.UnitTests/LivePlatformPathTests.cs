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
    public async Task Official_oauth_refreshes_an_expired_access_token()
    {
        var config = GoogleConfig();
        var broker = new OfficialOAuthBroker(new TokenHttpFactory("""{"access_token":"fresh-access","expires_in":3600}"""), config);
        var connection = Live("GOOGLE", expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(-5), refreshToken: "refresh-1", accessToken: "old-access");
        await broker.EnsureFreshAsync(connection, CancellationToken.None);
        Assert.Equal("fresh-access", connection.AccessToken);
        Assert.Equal("refresh-1", connection.RefreshToken);
        Assert.True(connection.HasLiveCredential);
        Assert.Equal(ConnectionStatus.Connected, connection.Status);
    }

    [Fact]
    public async Task Official_oauth_refresh_failure_does_not_invent_a_grant()
    {
        var broker = new OfficialOAuthBroker(new TokenHttpFactory("{}", status: System.Net.HttpStatusCode.BadRequest), GoogleConfig());
        var connection = Live("GOOGLE", expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1), refreshToken: "refresh-1", accessToken: "old-access");
        await broker.EnsureFreshAsync(connection, CancellationToken.None);
        Assert.Equal("old-access", connection.AccessToken);
        Assert.Equal(ConnectionStatus.NeedsReauth, connection.Status);
        Assert.False(connection.HasLiveCredential);
        Assert.Contains("did not invent", connection.LastError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Official_oauth_does_not_refresh_a_valid_token()
    {
        var broker = new OfficialOAuthBroker(new UnusedHttpFactory(), GoogleConfig());
        var connection = Live("GOOGLE", expiresAtUtc: DateTimeOffset.UtcNow.AddHours(1), refreshToken: "refresh-1", accessToken: "old-access");
        await broker.EnsureFreshAsync(connection, CancellationToken.None);
        Assert.Equal("old-access", connection.AccessToken);
        Assert.Equal(ConnectionStatus.Connected, connection.Status);
    }

    [Fact]
    public async Task Official_oauth_expired_without_refresh_token_needs_reauth()
    {
        var broker = new OfficialOAuthBroker(new UnusedHttpFactory(), GoogleConfig());
        var connection = Live("GOOGLE", expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1), refreshToken: null);
        await broker.EnsureFreshAsync(connection, CancellationToken.None);
        Assert.Equal(ConnectionStatus.NeedsReauth, connection.Status);
        Assert.False(connection.HasLiveCredential);
    }

    [Fact]
    public async Task Official_oauth_expired_without_keys_does_not_invent_a_refresh()
    {
        var broker = new OfficialOAuthBroker(new UnusedHttpFactory(), EmptyConfig());
        var connection = Live("GOOGLE", expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1), refreshToken: "refresh-1", accessToken: "old-access");
        await broker.EnsureFreshAsync(connection, CancellationToken.None);
        Assert.Equal("old-access", connection.AccessToken);
        Assert.Equal(ConnectionStatus.NeedsReauth, connection.Status);
        Assert.Contains("not configured", connection.LastError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Ads_metrics_hold_without_developer_token()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var adapter = new GoogleAdsAdapter(new FakeGateway(), config);
        var connection = Live("GOOGLE_ADS");
        var result = await adapter.MetricsAsync(connection, CancellationToken.None);
        Assert.Equal("Hold", result.Status);
        Assert.Contains("DeveloperToken", result.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not invented", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Ads_metrics_return_official_campaigns_only()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Connections:GoogleAds:DeveloperToken"] = "dev-token"
        }).Build();
        var adapter = new GoogleAdsAdapter(new AdsGateway(), config);
        var connection = Live("GOOGLE_ADS");
        var result = await adapter.MetricsAsync(connection, CancellationToken.None);
        Assert.Equal("Observed", result.Status);
        var campaigns = DigitalPulse.Application.Website.GoogleAdsCampaigns.Parse(result.Detail);
        Assert.Single(campaigns);
        Assert.Equal("Brand", campaigns[0].Name);
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

    private static PlatformConnection Live(string platform, DateTimeOffset? expiresAtUtc = null, string? refreshToken = null, string accessToken = "access-token")
    {
        var connection = PlatformConnection.Start(Guid.NewGuid(), Guid.NewGuid(), platform, PlatformAuthMode.OAuth, "state-live");
        connection.AttachLiveGrant("OAuth", "oauth-test", platform, accessToken, refreshToken, expiresAtUtc, "test");
        return connection;
    }

    private static IConfiguration EmptyConfig() => new ConfigurationBuilder().AddInMemoryCollection().Build();

    private static IConfiguration GoogleConfig() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Connections:Google:ClientId"] = "gid.apps.googleusercontent.com",
        ["Connections:Google:ClientSecret"] = "gsecret"
    }).Build();

    private sealed class AdsGateway : IOfficialPlatformGateway
    {
        public Task<OfficialHttpResult> SendAsync(
            HttpMethod method,
            string url,
            string? accessToken,
            string? jsonBody,
            IReadOnlyDictionary<string, string>? headers,
            CancellationToken cancellationToken)
        {
            if (url.Contains("listAccessibleCustomers", StringComparison.Ordinal))
            {
                return Task.FromResult(new OfficialHttpResult(200, """{"resourceNames":["customers/123"]}""", true));
            }

            if (url.Contains("googleAds:search", StringComparison.Ordinal))
            {
                return Task.FromResult(new OfficialHttpResult(200, """{"results":[{"campaign":{"id":"9","name":"Brand","status":"ENABLED"},"metrics":{"impressions":"10","clicks":"1"}}]}""", true));
            }

            return Task.FromResult(new OfficialHttpResult(404, "{}", false));
        }
    }

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

    private sealed class TokenHttpFactory(string body, System.Net.HttpStatusCode status = System.Net.HttpStatusCode.OK) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new TokenHandler(body, status), disposeHandler: false);
    }

    private sealed class TokenHandler(string body, System.Net.HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
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
