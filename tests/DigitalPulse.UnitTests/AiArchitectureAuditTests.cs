using DigitalPulse.Application.Abstractions;
using DigitalPulse.Infrastructure.Ai;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class AiArchitectureAuditTests
{
    [Fact]
    public void Development_hold_is_not_a_live_model()
    {
        IAiProvider provider = new DevelopmentAiProvider();
        Assert.False(provider.IsLive);
        Assert.Equal("Development", provider.ProviderName);
    }

    [Fact]
    public void Vendor_clients_live_only_in_infrastructure()
    {
        Assert.Equal("DigitalPulse.Infrastructure", typeof(OpenAiProvider).Assembly.GetName().Name);
        Assert.Equal("DigitalPulse.Application", typeof(IAiProvider).Assembly.GetName().Name);
        Assert.DoesNotContain(
            typeof(IAiProvider).Assembly.GetTypes(),
            type => type.Name.Contains("OpenAi", StringComparison.OrdinalIgnoreCase));
    }
}
