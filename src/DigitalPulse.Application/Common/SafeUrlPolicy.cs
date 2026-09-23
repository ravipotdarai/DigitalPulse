using System.Net;
using System.Net.Sockets;

namespace DigitalPulse.Application.Common;

public static class SafeUrlPolicy
{
    private static readonly HashSet<string> BlockedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "localhost.localdomain",
        "metadata.google.internal",
        "metadata",
        "0.0.0.0",
        "[::]",
        "[::1]"
    };

    public static bool TryValidate(string? raw, out Uri uri, out string error)
    {
        uri = null!;
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "Website URL is empty.";
            return false;
        }

        var candidate = raw.Trim();
        if (!candidate.Contains("://", StringComparison.Ordinal))
        {
            candidate = "https://" + candidate;
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var parsed) || parsed is null)
        {
            error = "Website URL is not a valid absolute URI.";
            return false;
        }

        if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
        {
            error = "Website URL must use http or https.";
            return false;
        }

        if (!string.IsNullOrEmpty(parsed.UserInfo))
        {
            error = "Website URL must not include credentials.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(parsed.Host) || parsed.IsLoopback || IsBlockedHost(parsed.Host))
        {
            error = "Website host is not allowed.";
            return false;
        }

        if (IPAddress.TryParse(parsed.Host.Trim('[', ']'), out var literal) && !IsPublicAddress(literal))
        {
            error = "Website host resolves to a private or reserved address.";
            return false;
        }

        uri = parsed;
        return true;
    }

    public static bool IsBlockedHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return true;
        }

        var trimmed = host.Trim().TrimEnd('.');
        if (BlockedHosts.Contains(trimmed))
        {
            return true;
        }

        return trimmed.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith(".internal", StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPublicAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)
            || address.Equals(IPAddress.Any)
            || address.Equals(IPAddress.IPv6Any)
            || address.IsIPv6LinkLocal
            || address.IsIPv6Multicast
            || address.IsIPv6SiteLocal)
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] switch
            {
                0 => false,
                10 => false,
                127 => false,
                169 when bytes[1] == 254 => false,
                172 when bytes[1] is >= 16 and <= 31 => false,
                192 when bytes[1] == 168 => false,
                100 when bytes[1] is >= 64 and <= 127 => false,
                _ => true
            };
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv4MappedToIPv6)
            {
                return IsPublicAddress(address.MapToIPv4());
            }

            var bytes = address.GetAddressBytes();
            return bytes[0] is not (0xFC or 0xFD or 0xFE or 0xFF);
        }

        return false;
    }
}
