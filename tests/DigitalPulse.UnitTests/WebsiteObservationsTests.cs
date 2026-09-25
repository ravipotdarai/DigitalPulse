using DigitalPulse.Application.Website;
using DigitalPulse.Domain.Platforms;
using DigitalPulse.Domain.Website;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class WebsiteObservationsTests
{
    [Fact]
    public void Thin_homepage_produces_seo_and_aeo_without_invented_rankings()
    {
        var snapshot = WebsiteSnapshot.Record(
            Guid.NewGuid(), Guid.NewGuid(), "https://harbour.example/",
            WebsiteFetchStatus.Reached, 200, null, null, null, null, null,
            false, false, false, false, 12, false, false, null);
        var signals = HtmlSignalParser.Parse("<html><body><p>Welcome.</p></body></html>");

        var items = WebsiteObservations.FromSnapshot("Harbour Coffee", snapshot, signals, []);
        Assert.Contains(items, i => i.Title.Contains("Title tag", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(items, i => i.Category == SearchObservationCategory.Aeo);
        Assert.Contains(items, i => i.Category == SearchObservationCategory.SearchConsole);
        Assert.Contains(items, i => i.Title.Contains("Search Console is not connected", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(items, i => i.Detail.Contains("impression", StringComparison.OrdinalIgnoreCase) && i.ObservedValue?.Any(char.IsDigit) == true);
        Assert.DoesNotContain(items, i => i.Title.Contains("rank", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(items, i => i.Title.Contains("no live reader", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Development_search_console_is_unavailable_not_fabricated()
    {
        var snapshot = WebsiteSnapshot.Record(
            Guid.NewGuid(), Guid.NewGuid(), "https://harbour.example/",
            WebsiteFetchStatus.Reached, 200, "Harbour Coffee", "House roasted coffee in Mumbai for weekday mornings.",
            "Harbour Coffee", "https://harbour.example/", "index,follow",
            true, true, true, true, 240, true, true, null);
        var connection = PlatformConnection.Start(snapshot.TenantId, snapshot.BusinessId, "SEARCH_CONSOLE", PlatformAuthMode.OAuth, "state-gsc");
        connection.MarkConnected("Development", "dev-ref", "dev-account");

        var items = WebsiteObservations.FromSnapshot("Harbour Coffee", snapshot, null, [connection]);
        Assert.Contains(items, i => i.Title.Contains("Search Console snapshot is unavailable", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(items, i => i.ObservedValue?.Contains("clicks", StringComparison.OrdinalIgnoreCase) == true);
    }
}
