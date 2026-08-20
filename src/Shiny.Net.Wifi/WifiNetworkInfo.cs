using System.Net;
using System.Net.Sockets;
using Shiny.Net.Wifi.Internals;

namespace Shiny.Net.Wifi;


/// <summary>
/// The network the device is currently joined to, and how it is addressed on it.
/// </summary>
/// <remarks>
/// <para>The addressing half (<see cref="IpAddresses"/>, <see cref="DnsAddresses"/>,
/// <see cref="Gateway"/>) comes from the managed network stack and is available on every platform.
/// The Wi-Fi half (<see cref="Ssid"/>, <see cref="Bssid"/>, <see cref="SignalStrengthDbm"/>) needs
/// a platform permission and is null where that permission is missing - see
/// <see cref="WifiCapabilities.CurrentNetwork"/>.</para>
/// <para>Equality compares the address lists element by element, not by reference, so this is safe
/// to use for change detection.</para>
/// </remarks>
public sealed record WifiNetworkInfo
{
    /// <summary>The OS name of the interface, eg "wlan0", "en0" or "Wi-Fi".</summary>
    public required string InterfaceName { get; init; }

    /// <summary>The network name, or null when the platform will not disclose it.</summary>
    public string? Ssid { get; init; }

    /// <summary>The MAC address of the access point, or null when unavailable.</summary>
    public string? Bssid { get; init; }

    /// <summary>The authentication scheme in use.</summary>
    public WifiSecurity Security { get; init; } = WifiSecurity.Unknown;

    /// <summary>Every address assigned to the interface, IPv4 and IPv6.</summary>
    public IReadOnlyList<IPAddress> IpAddresses { get; init; } = Array.Empty<IPAddress>();

    /// <summary>The DNS resolvers configured for the interface.</summary>
    public IReadOnlyList<IPAddress> DnsAddresses { get; init; } = Array.Empty<IPAddress>();

    /// <summary>The default gateway, or null when there is no default route on this interface.</summary>
    public IPAddress? Gateway { get; init; }

    /// <summary>The IPv4 subnet mask, or null when unavailable.</summary>
    public IPAddress? SubnetMask { get; init; }

    /// <summary>Signal strength in dBm - typically -30 (excellent) to -90 (unusable).</summary>
    public int? SignalStrengthDbm { get; init; }

    /// <summary>Signal strength as 0-100, derived from <see cref="SignalStrengthDbm"/> where the platform does not supply it directly.</summary>
    public int? SignalStrengthPercent { get; init; }

    /// <summary>The centre frequency in MHz.</summary>
    public int? FrequencyMhz { get; init; }

    /// <summary>The band derived from <see cref="FrequencyMhz"/>.</summary>
    public WifiBand Band => WifiChannels.ToBand(this.FrequencyMhz);

    /// <summary>The channel number derived from <see cref="FrequencyMhz"/>.</summary>
    public int? Channel => WifiChannels.ToChannel(this.FrequencyMhz);

    /// <summary>The first IPv4 address on the interface, or null if it has none.</summary>
    public IPAddress? IPv4Address => this.IpAddresses.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);

    /// <summary>The first IPv6 address on the interface, or null if it has none.</summary>
    public IPAddress? IPv6Address => this.IpAddresses.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetworkV6);


    // the compiler-generated members would compare the two lists by reference, which makes every
    // poll look like a change - the whole point of this type is to be diffed
    public bool Equals(WifiNetworkInfo? other)
        => other != null &&
           this.InterfaceName == other.InterfaceName &&
           this.Ssid == other.Ssid &&
           this.Bssid == other.Bssid &&
           this.Security == other.Security &&
           this.SignalStrengthDbm == other.SignalStrengthDbm &&
           this.SignalStrengthPercent == other.SignalStrengthPercent &&
           this.FrequencyMhz == other.FrequencyMhz &&
           Equals(this.Gateway, other.Gateway) &&
           Equals(this.SubnetMask, other.SubnetMask) &&
           this.IpAddresses.SequenceEqual(other.IpAddresses) &&
           this.DnsAddresses.SequenceEqual(other.DnsAddresses);


    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(this.InterfaceName);
        hash.Add(this.Ssid);
        hash.Add(this.Bssid);
        hash.Add(this.Security);
        hash.Add(this.FrequencyMhz);

        foreach (var ip in this.IpAddresses)
            hash.Add(ip);

        foreach (var dns in this.DnsAddresses)
            hash.Add(dns);

        return hash.ToHashCode();
    }
}
