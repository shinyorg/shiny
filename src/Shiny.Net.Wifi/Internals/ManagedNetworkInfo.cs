using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Shiny.Net.Wifi.Internals;


/// <summary>
/// Builds the addressing half of a <see cref="WifiNetworkInfo"/> from the managed network stack.
/// </summary>
/// <remarks>
/// IP, DNS, gateway and mask are the same everywhere and System.Net.NetworkInformation reads them
/// on every platform Shiny targets, so only the Wi-Fi half (SSID, BSSID, RSSI) needs native code.
/// Android is the exception and uses LinkProperties instead - it is the only platform that scopes
/// the answer to the network the app is actually bound to.
/// </remarks>
internal static class ManagedNetworkInfo
{
    /// <summary>
    /// Reads the addressing for a wireless interface.
    /// </summary>
    /// <param name="interfaceName">
    /// The interface to read, matched on Name or Id. Null picks the first wireless interface that
    /// is up, which is the right answer on every device with a single Wi-Fi radio.
    /// </param>
    /// <returns>Addressing only - SSID, BSSID and signal are left for the caller to fill in.</returns>
    public static WifiNetworkInfo? Read(string? interfaceName = null)
    {
        var nic = Find(interfaceName);
        return nic == null ? null : Build(nic);
    }


    public static NetworkInterface? Find(string? interfaceName)
    {
        var all = NetworkInterface.GetAllNetworkInterfaces();

        if (!String.IsNullOrWhiteSpace(interfaceName))
        {
            return all.FirstOrDefault(x =>
                x.Name.Equals(interfaceName, StringComparison.OrdinalIgnoreCase) ||
                x.Id.Equals(interfaceName, StringComparison.OrdinalIgnoreCase)
            );
        }

        return all.FirstOrDefault(x =>
            x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 &&
            x.OperationalStatus == OperationalStatus.Up
        );
    }


    // CA1416: the analyzer flags DnsAddresses and GatewayAddresses as unimplemented on Android and
    // iOS. That is exactly why each of them is read through Optional, and why Android never routes
    // through here at all - it reads LinkProperties instead.
#pragma warning disable CA1416
    public static WifiNetworkInfo Build(NetworkInterface nic)
    {
        var props = TryGetProperties(nic);
        var unicast = Optional(() => props?.UnicastAddresses.ToArray()) ?? Array.Empty<UnicastIPAddressInformation>();

        return new WifiNetworkInfo
        {
            InterfaceName = nic.Name,
            IpAddresses = unicast.Select(x => x.Address).ToArray(),
            DnsAddresses = Optional(() => props?.DnsAddresses.ToArray()) ?? Array.Empty<IPAddress>(),
            Gateway = Optional(() => props?
                .GatewayAddresses
                .Select(x => x.Address)
                .FirstOrDefault(x => !x.Equals(IPAddress.Any) && !x.Equals(IPAddress.IPv6Any))),
            SubnetMask = Optional(() => unicast
                .FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork)?
                .IPv4Mask)
        };
    }
#pragma warning restore CA1416


    /// <summary>
    /// Reads one property that the platform is allowed not to implement, and returns null when it
    /// will not answer.
    /// </summary>
    /// <remarks>
    /// Per property rather than around the block, because these fail independently: the platforms
    /// are inconsistent about <em>which</em> of them they implement, and one refusal must not cost
    /// the caller the fields that were readable. It is also why guarding
    /// <see cref="NetworkInterface.GetIPProperties"/> alone is not enough - that call succeeds and
    /// the refusal arrives later, off the property. GatewayAddresses is
    /// <c>[UnsupportedOSPlatform("android")]</c> and throws even where /proc/net/route exists, and
    /// on Linux an unreadable route file behaves the same way.
    /// </remarks>
    static T? Optional<T>(Func<T?> read) where T : class
    {
        try
        {
            return read();
        }
        catch (PlatformNotSupportedException)
        {
            return null;
        }
        catch (NetworkInformationException)
        {
            return null;
        }
    }


    /// <remarks>
    /// The factory call itself, which a sandboxed platform can refuse outright. A refusal that
    /// arrives later - off one of the individual properties - is <see cref="Optional{T}"/>'s job,
    /// and is the more common of the two.
    /// </remarks>
    static IPInterfaceProperties? TryGetProperties(NetworkInterface nic)
    {
        try
        {
            return nic.GetIPProperties();
        }
        catch (PlatformNotSupportedException)
        {
            return null;
        }
        catch (NetworkInformationException)
        {
            return null;
        }
    }
}
