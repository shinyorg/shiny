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
    // iOS. That is exactly why TryGetProperties swallows PlatformNotSupportedException, and why
    // Android never routes through here at all - it reads LinkProperties instead.
#pragma warning disable CA1416
    public static WifiNetworkInfo Build(NetworkInterface nic)
    {
        var props = TryGetProperties(nic);
        var unicast = props?.UnicastAddresses.ToArray() ?? Array.Empty<UnicastIPAddressInformation>();

        return new WifiNetworkInfo
        {
            InterfaceName = nic.Name,
            IpAddresses = unicast.Select(x => x.Address).ToArray(),
            DnsAddresses = props?.DnsAddresses.ToArray() ?? Array.Empty<IPAddress>(),
            Gateway = props?
                .GatewayAddresses
                .Select(x => x.Address)
                .FirstOrDefault(x => !x.Equals(IPAddress.Any) && !x.Equals(IPAddress.IPv6Any)),
            SubnetMask = unicast
                .FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork)?
                .IPv4Mask
        };
    }
#pragma warning restore CA1416


    /// <remarks>
    /// The sandboxed platforms are inconsistent about which of these properties they implement -
    /// iOS in particular throws PlatformNotSupportedException off DnsAddresses on some releases.
    /// A partial answer beats an exception here, since the SSID half is what callers came for.
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
