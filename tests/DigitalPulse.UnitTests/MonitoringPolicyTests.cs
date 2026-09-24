using DigitalPulse.Domain.Monitoring;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class MonitoringPolicyTests
{
    [Theory]
    [InlineData("STARTER", 168)]
    [InlineData("GROWTH", 24)]
    [InlineData("BUSINESS", 6)]
    [InlineData("AGENCY", 1)]
    public void Plan_interval_follows_the_catalog(string code, int hours)
    {
        Assert.Equal(hours, MonitoringPolicy.IntervalHoursFor(code));
    }

    [Fact]
    public void Due_when_next_run_is_missing_or_past()
    {
        Assert.True(MonitoringPolicy.IsDue(null));
        Assert.True(MonitoringPolicy.IsDue(DateTimeOffset.UtcNow.AddMinutes(-1)));
        Assert.False(MonitoringPolicy.IsDue(DateTimeOffset.UtcNow.AddHours(2)));
    }

    [Fact]
    public void Live_metric_kinds_cannot_observe_without_an_api()
    {
        Assert.Contains(MonitoringCatalog.All, k => k.Kind == MonitoringKind.SearchVisibility && !k.CanObserveWithoutLiveApi);
        Assert.Contains(MonitoringCatalog.All, k => k.Kind == MonitoringKind.ReviewChanges && !k.CanObserveWithoutLiveApi);
        Assert.Contains(MonitoringCatalog.All, k => k.Kind == MonitoringKind.CompetitorChanges && !k.CanObserveWithoutLiveApi);
        Assert.Contains(MonitoringCatalog.All, k => k.Kind == MonitoringKind.ApiFailures && !k.CanObserveWithoutLiveApi);
        Assert.Contains(MonitoringCatalog.All, k => k.Kind == MonitoringKind.WebsiteAvailability && k.CanObserveWithoutLiveApi);
    }

    [Fact]
    public void Report_keeps_decision_off_the_observed_fact()
    {
        var report = PresenceReport.Assemble(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ReportKind.Pulse,
            "Presence pulse",
            "Website reached HTTP 200.",
            "Keep the homepage reachable.",
            "No AI interpretation. This report is assembled from stored observations only.",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            "Live provider metrics stay held.");
        Assert.Null(report.CustomerDecision);
        report.RecordDecision("Accepted. We will fix the listing next week.");
        Assert.Equal("Accepted. We will fix the listing next week.", report.CustomerDecision);
        Assert.Equal("Website reached HTTP 200.", report.ObservedFact);
        Assert.Contains("No AI interpretation", report.AiInterpretation, StringComparison.Ordinal);
    }
}
