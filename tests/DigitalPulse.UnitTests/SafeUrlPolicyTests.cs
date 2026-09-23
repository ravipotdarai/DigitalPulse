using System.Net;
using DigitalPulse.Application.Common;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class SafeUrlPolicyTests
{
    [Theory]
    [InlineData("http://localhost")]
    [InlineData("https://127.0.0.1")]
    [InlineData("https://10.0.0.8/admin")]
    [InlineData("https://192.168.1.10")]
    [InlineData("https://169.254.169.254/latest/meta-data")]
    [InlineData("file:///etc/passwd")]
    [InlineData("https://user:pass@example.com")]
    public void Rejects_unsafe_urls(string url)
    {
        Assert.False(SafeUrlPolicy.TryValidate(url, out _, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Accepts_public_https_host()
    {
        Assert.True(SafeUrlPolicy.TryValidate("https://harbour.example/menu", out var uri, out _));
        Assert.Equal(Uri.UriSchemeHttps, uri.Scheme);
        Assert.Equal("harbour.example", uri.Host);
    }

    [Fact]
    public void Private_ipv4_is_not_public()
    {
        Assert.False(SafeUrlPolicy.IsPublicAddress(IPAddress.Parse("10.1.2.3")));
        Assert.False(SafeUrlPolicy.IsPublicAddress(IPAddress.Parse("172.16.0.1")));
        Assert.False(SafeUrlPolicy.IsPublicAddress(IPAddress.Loopback));
        Assert.True(SafeUrlPolicy.IsPublicAddress(IPAddress.Parse("1.1.1.1")));
    }
}
