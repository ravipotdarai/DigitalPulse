using DigitalPulse.Domain.WhatsApp;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class WhatsAppPolicyTests
{
    [Fact]
    public void Stored_number_without_opt_in_is_not_sendable()
    {
        var decision = WhatsAppPolicy.Evaluate(true, true, true, WhatsAppConsentStatus.Unknown, WhatsAppMessageKind.Template, true, false, true);
        Assert.False(decision.Allowed);
        Assert.Contains("opt-in", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Opt_out_blocks_immediately()
    {
        var decision = WhatsAppPolicy.Evaluate(true, true, true, WhatsAppConsentStatus.OptedOut, WhatsAppMessageKind.Template, true, true, true);
        Assert.False(decision.Allowed);
        Assert.Contains("Opt-out", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Business_initiated_send_requires_an_approved_template()
    {
        var decision = WhatsAppPolicy.Evaluate(true, true, true, WhatsAppConsentStatus.OptedIn, WhatsAppMessageKind.Template, false, false, true);
        Assert.False(decision.Allowed);
        Assert.Contains("approved WhatsApp message template", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Session_reply_requires_an_open_window()
    {
        Assert.False(WhatsAppPolicy.WindowOpen(DateTimeOffset.UtcNow.AddHours(-25)));
        Assert.True(WhatsAppPolicy.WindowOpen(DateTimeOffset.UtcNow.AddHours(-2)));
        var decision = WhatsAppPolicy.Evaluate(true, true, true, WhatsAppConsentStatus.OptedIn, WhatsAppMessageKind.Session, false, false, true);
        Assert.False(decision.Allowed);
        Assert.Contains("24-hour", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Session_reply_inside_the_window_can_hold_without_a_live_api()
    {
        var decision = WhatsAppPolicy.Evaluate(true, true, false, WhatsAppConsentStatus.OptedIn, WhatsAppMessageKind.Session, false, true, false);
        Assert.True(decision.Allowed);
        Assert.False(decision.CanSendLive);
        Assert.Contains("not configured", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Starter_plan_cannot_use_whatsapp()
    {
        var decision = WhatsAppPolicy.Evaluate(false, true, true, WhatsAppConsentStatus.OptedIn, WhatsAppMessageKind.Template, true, false, false);
        Assert.False(decision.Allowed);
        Assert.Contains("does not include WhatsApp", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
