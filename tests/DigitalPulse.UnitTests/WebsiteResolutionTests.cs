using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Features.Scans;
using DigitalPulse.Application.Website;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Scans;
using DigitalPulse.Domain.Website;
using DigitalPulse.Infrastructure.Reports;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class WebsiteResolutionTests
{
    [Fact]
    public void Classifies_about_and_contact_from_url()
    {
        Assert.Equal(WebsitePageRole.About, WebsitePageRoles.Classify("https://harbour.example/about-us/", "About us", null));
        Assert.Equal(WebsitePageRole.Contact, WebsitePageRoles.Classify("https://harbour.example/contact/", "Contact", null));
        Assert.Equal(WebsitePageRole.Home, WebsitePageRoles.Classify("https://harbour.example/", "Harbour Coffee", null));
    }

    [Fact]
    public void Same_host_only_and_never_invents_offsite_pages()
    {
        var origin = new Uri("https://harbour.example/");
        Assert.True(WebsiteSiteCrawler.TryNormalize(origin, "/about", out var about));
        Assert.Contains("harbour.example", about, StringComparison.OrdinalIgnoreCase);
        Assert.False(WebsiteSiteCrawler.TryNormalize(origin, "https://other.example/about", out _));
        Assert.False(WebsiteSiteCrawler.TryNormalize(origin, "mailto:hi@harbour.example", out _));
        Assert.Null(Record.Exception(() => WebsiteSiteCrawler.TryNormalize(origin, "/files/quote*sheet.pdf", out _)));
        Assert.False(WebsiteSiteCrawler.TryNormalize(origin, "/brand.jpg", out _));
    }

    [Fact]
    public void Html_parse_survives_empty_and_does_not_require_windows_paths()
    {
        var signals = HtmlSignalParser.Parse("<html><title>Harbour Coffee</title><h1>Welcome</h1><a href=\"/about\">About</a></html>");
        Assert.Equal("Harbour Coffee", signals.Title);
        Assert.Contains("/about*team", HtmlSignalParser.ExtractHrefs("<a href=\"/about*team\">About</a>"));
    }

    [Fact]
    public void Missing_about_and_contact_are_findings_not_invented_pages()
    {
        var tenant = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var business = Business.Create(tenant, "Harbour Coffee", "https://harbour.example");
        var home = WebsiteSnapshot.Record(
            tenant, businessId, "https://harbour.example/",
            WebsiteFetchStatus.Reached, 200, "Harbour Coffee", null, "Harbour Coffee", null, null,
            false, false, false, false, 40, true, false, null,
            new WebsitePageContext(WebsitePageRole.Home, Guid.NewGuid()));

        var items = WebsitePageAudits.FromPages(business, [home], [], [], []);
        Assert.Contains(items, i => i.Title.Contains("About page was not found", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(items, i => i.Title.Contains("Contact page was not found", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(items, i => i.Title.Contains("product", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Official_gsc_json_parses_rows_empty_stays_empty()
    {
        var rows = SearchConsoleQueries.Parse("""{"rows":[{"keys":["best coffee"],"clicks":3,"impressions":40,"ctr":0.075,"position":8.2}]}""");
        Assert.Single(rows);
        Assert.Equal("best coffee", rows[0].Query);
        Assert.Equal(3, rows[0].Clicks);
        Assert.Empty(SearchConsoleQueries.Parse("""{"rows":[]}"""));
        Assert.Empty(SearchConsoleQueries.Parse("not-json"));
    }

    [Fact]
    public void Official_ads_json_parses_campaigns_empty_stays_empty()
    {
        var rows = GoogleAdsCampaigns.Parse("""{"results":[{"campaign":{"id":"111","name":"Brand","status":"ENABLED"},"metrics":{"impressions":"12","clicks":"2"}}]}""");
        Assert.Single(rows);
        Assert.Equal("111", rows[0].Id);
        Assert.Equal("Brand", rows[0].Name);
        Assert.Empty(GoogleAdsCampaigns.Parse("""{"results":[]}"""));
        Assert.Empty(GoogleAdsCampaigns.Parse("not-json"));
        Assert.Empty(GoogleAdsCampaigns.CustomerIds("""{"resourceNames":[]}"""));
        Assert.Equal("123", GoogleAdsCampaigns.CustomerIds("""{"resourceNames":["customers/123"]}""")[0]);
    }

    [Fact]
    public void Official_ga4_properties_parse_without_inventing()
    {
        var rows = AnalyticsProperties.Parse("""{"accountSummaries":[{"displayName":"Harbour","propertySummaries":[{"property":"properties/99","displayName":"Web"}]}]}""");
        Assert.Single(rows);
        Assert.Equal("properties/99", rows[0].Property);
        Assert.Empty(AnalyticsProperties.Parse("""{"accountSummaries":[]}"""));
        Assert.Empty(AnalyticsProperties.Parse("not-json"));
    }

    [Fact]
    public void Check_ads_findings_come_from_official_campaigns_only()
    {
        var report = TestReport.Assemble(
            Guid.NewGuid(), Guid.NewGuid(), TestReportKind.GoogleAds, "Google Ads",
            "1 official campaign(s) stored.", "Review official rows.", string.Empty,
            """{"results":[{"campaign":{"id":"9","name":"Search","status":"PAUSED"},"metrics":{"impressions":"4","clicks":"1"}}]}""",
            Guid.NewGuid());
        var findings = CheckComparisons.CompareGoogleMetrics("Ads", "Google Ads", report);
        Assert.Single(findings);
        Assert.Contains("Search", findings[0].Title, StringComparison.Ordinal);
        Assert.DoesNotContain(findings, f => f.Title.Contains("invent", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Pdf_includes_heading_tagline_date_and_user()
    {
        var report = TestReport.Assemble(
            Guid.NewGuid(), Guid.NewGuid(), TestReportKind.WebsiteAudit, "Website audit",
            "Home reached.", "Fix About.", string.Empty, "Home: https://harbour.example/", Guid.NewGuid());
        var bytes = new TestReportPdf().Render(report, new TestReportPdfHeader("Manav Shah", new DateTimeOffset(2026, 9, 25, 10, 0, 0, TimeSpan.Zero)));
        var text = System.Text.Encoding.ASCII.GetString(bytes);
        Assert.Contains("DigitalPulse", text, StringComparison.Ordinal);
        Assert.Contains("AI Digital Presence OS", text, StringComparison.Ordinal);
        Assert.Contains("Manav Shah", text, StringComparison.Ordinal);
        Assert.Contains("25 Sep 2026", text, StringComparison.Ordinal);
        Assert.Contains("%PDF", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Finding_cannot_resolve_before_verify()
    {
        var finding = Finding.Open(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Website", FindingSeverity.Medium, "About page was not found",
            "The crawl did not find About.", "About page", "Missing",
            "Add About", "Add About", "Re-analyze", FindingAutomationState.Assisted);
        Assert.Throws<InvalidOperationException>(() => finding.SetStatus(FindingStatus.Resolved));
        finding.MarkVerified();
        finding.SetStatus(FindingStatus.Resolved);
        Assert.Equal(FindingStatus.Resolved, finding.Status);
    }
}
