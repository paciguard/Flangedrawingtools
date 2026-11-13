using System.Net;
using Microsoft.AspNetCore.Http;

namespace FlangeWeb;

public static class ShutdownPolicy
{
    public static bool IsAllowed(HttpContext context)
    {
        var remote = context.Connection.RemoteIpAddress;
        if (remote is null) return false;
        if (remote.IsIPv4MappedToIPv6) remote = remote.MapToIPv4();
        if (!IPAddress.IsLoopback(remote)) return false;

        var host = context.Request.Host.Host.Trim('[', ']');
        if (!host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
            (!IPAddress.TryParse(host, out var address) || !IPAddress.IsLoopback(address))) return false;
        if (!context.Request.HasJsonContentType() || context.Request.Headers["X-Flange-Action"] != "exit") return false;
        var origin = context.Request.Headers.Origin.ToString();
        var expected = context.Request.Scheme + "://" + context.Request.Host.Value;
        return origin.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }
}
