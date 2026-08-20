using Shiny.Net.Wifi.Internals;

namespace Shiny.Net.Wifi;


/// <summary>
/// An access point seen by <see cref="IWifiManager.Scan"/>.
/// </summary>
/// <remarks>
/// One SSID can appear several times - once per radio of a multi-band or mesh network - so results
/// are unique on <see cref="Bssid"/>, not <see cref="Ssid"/>. Group by SSID yourself if you are
/// building a picker, and take the strongest BSSID of each group.
/// </remarks>
public sealed record WifiNetwork
{
    /// <summary>The network name. Empty for a hidden network that did not broadcast it.</summary>
    public required string Ssid { get; init; }

    /// <summary>The MAC address of the radio that answered, or null where the platform withholds it.</summary>
    public string? Bssid { get; init; }

    /// <summary>The authentication scheme advertised.</summary>
    public WifiSecurity Security { get; init; } = WifiSecurity.Unknown;

    /// <summary>Signal strength in dBm - typically -30 (excellent) to -90 (unusable).</summary>
    public int? SignalStrengthDbm { get; init; }

    /// <summary>Signal strength as 0-100, for direct display.</summary>
    public int SignalStrengthPercent { get; init; }

    /// <summary>The centre frequency in MHz, or null where unreported.</summary>
    public int? FrequencyMhz { get; init; }

    /// <summary>True when the access point does not broadcast its SSID.</summary>
    public bool IsHidden { get; init; }

    /// <summary>The band derived from <see cref="FrequencyMhz"/>.</summary>
    public WifiBand Band => WifiChannels.ToBand(this.FrequencyMhz);

    /// <summary>The channel number derived from <see cref="FrequencyMhz"/>.</summary>
    public int? Channel => WifiChannels.ToChannel(this.FrequencyMhz);

    /// <summary>True when joining requires no passphrase. WEP counts as open - it is not security.</summary>
    public bool IsOpen => this.Security is WifiSecurity.Open or WifiSecurity.Owe;
}
