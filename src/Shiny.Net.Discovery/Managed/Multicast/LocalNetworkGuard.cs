using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Shiny.Net.Discovery.Managed.Multicast;


/// <summary>
/// Decides whether an address is on a network we actually belong to, and whether a URL harvested
/// from a discovery datagram is safe to fetch.
/// </summary>
/// <remarks>
/// Discovery payloads are unauthenticated datagrams from anyone who can reach the multicast group,
/// and both SSDP and WS-Discovery hand out URLs (<c>LOCATION</c>, <c>XAddrs</c>) that this library
/// will then fetch. Without a policy that is a server-side request forgery primitive, so the
/// default is deny: a literal-IP host must match the address that sent the datagram.
/// </remarks>
static class LocalNetworkGuard
{
    /// <summary>How long a snapshot of the local interface table is reused before rebuilding.</summary>
    static readonly TimeSpan snapshotLifetime = TimeSpan.FromSeconds(15);

    static readonly Lock sync = new();
    static List<Subnet>? snapshot;
    static long snapshotTaken;


    /// <summary>
    /// True when the address is one this host could plausibly talk to directly - loopback,
    /// link-local, or inside the prefix of one of our own interfaces.
    /// </summary>
    public static bool IsOnLocalNetwork(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
            return true;

        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || IsIPv4LinkLocal(address))
            return true;

        foreach (var subnet in GetSubnets())
        {
            if (subnet.Contains(address))
                return true;
        }
        return false;
    }


    /// <summary>
    /// Applies the default fetch policy to a URL advertised by a device.
    /// </summary>
    /// <param name="url">The advertised URL.</param>
    /// <param name="source">The address the advertisement arrived from.</param>
    /// <param name="reason">Why it was rejected, for logging.</param>
    public static bool IsFetchable(Uri url, IPAddress source, out string? reason)
    {
        reason = null;

        if (!url.IsAbsoluteUri)
        {
            reason = "the URL is not absolute";
            return false;
        }

        if (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
        {
            reason = $"the scheme '{url.Scheme}' is not http or https";
            return false;
        }

        if (!String.IsNullOrEmpty(url.UserInfo))
        {
            reason = "the URL carries credentials";
            return false;
        }

        if (!IPAddress.TryParse(url.DnsSafeHost, out var host))
        {
            // a DNS name could resolve anywhere, including off-link, and re-resolve differently
            // between the check and the connection
            reason = $"the host '{url.DnsSafeHost}' is a name rather than a literal address";
            return false;
        }

        if (IPAddress.Any.Equals(host) || IPAddress.IPv6Any.Equals(host))
        {
            reason = "the host is the unspecified address";
            return false;
        }

        if (IPAddress.IsLoopback(host) && !IPAddress.IsLoopback(source))
        {
            reason = "a remote device advertised a loopback address";
            return false;
        }

        // the single most effective control: the device that answered must be the device we call
        if (!host.Equals(source))
        {
            reason = $"the advertised host {host} is not the address {source} that sent the advertisement";
            return false;
        }
        return true;
    }


    /// <summary>
    /// Attaches the receiving interface's zone id to a link-local IPv6 address that arrived
    /// without one, which is otherwise unroutable.
    /// </summary>
    public static Uri ApplyZoneId(Uri url, int interfaceIndex)
    {
        if (interfaceIndex == 0 || !IPAddress.TryParse(url.DnsSafeHost, out var host))
            return url;

        if (host.AddressFamily != AddressFamily.InterNetworkV6 || !host.IsIPv6LinkLocal || host.ScopeId != 0)
            return url;

        host.ScopeId = interfaceIndex;
        var builder = new UriBuilder(url) { Host = $"[{host}]" };
        return builder.Uri;
    }


    static bool IsIPv4LinkLocal(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
            return false;

        var bytes = address.GetAddressBytes();
        return bytes[0] == 169 && bytes[1] == 254;
    }


    static IReadOnlyList<Subnet> GetSubnets()
    {
        lock (sync)
        {
            var now = Environment.TickCount64;
            if (snapshot != null && now - snapshotTaken < snapshotLifetime.TotalMilliseconds)
                return snapshot;

            snapshot = BuildSubnets();
            snapshotTaken = now;
            return snapshot;
        }
    }


    static List<Subnet> BuildSubnets()
    {
        var results = new List<Subnet>();
        try
        {
            var up = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(x => x.OperationalStatus == OperationalStatus.Up);

            foreach (var adapter in up)
            {
                foreach (var unicast in adapter.GetIPProperties().UnicastAddresses)
                {
                    var prefix = GetPrefixLength(unicast);
                    if (prefix > 0)
                        results.Add(new Subnet(unicast.Address, prefix));
                }
            }
        }
        catch (Exception)
        {
            // an interface can disappear mid-enumeration; whatever we collected is still usable
        }
        return results;
    }


    static int GetPrefixLength(UnicastIPAddressInformation unicast)
    {
        try
        {
            // PrefixLength throws on some platforms for some adapters, and reports 0 on others
            if (unicast.PrefixLength > 0)
                return unicast.PrefixLength;
        }
        catch (Exception)
        {
            // fall through to the mask
        }

        try
        {
            if (unicast.Address.AddressFamily == AddressFamily.InterNetwork && unicast.IPv4Mask != null)
            {
                return unicast.IPv4Mask
                    .GetAddressBytes()
                    .Sum(x => System.Numerics.BitOperations.PopCount(x));
            }
        }
        catch (Exception)
        {
            // no mask available
        }
        return 0;
    }


    readonly record struct Subnet(IPAddress Address, int PrefixLength)
    {
        public bool Contains(IPAddress candidate)
        {
            if (candidate.AddressFamily != this.Address.AddressFamily)
                return false;

            var mine = this.Address.GetAddressBytes();
            var theirs = candidate.GetAddressBytes();
            if (mine.Length != theirs.Length)
                return false;

            var bits = this.PrefixLength;
            for (var i = 0; i < mine.Length && bits > 0; i++)
            {
                var take = Math.Min(8, bits);
                var mask = (byte)(0xFF << (8 - take));

                if ((mine[i] & mask) != (theirs[i] & mask))
                    return false;

                bits -= take;
            }
            return true;
        }
    }
}
