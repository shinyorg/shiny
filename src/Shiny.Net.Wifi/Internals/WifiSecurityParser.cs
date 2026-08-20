namespace Shiny.Net.Wifi.Internals;


/// <summary>
/// Parses the bracketed capability string that Android's <c>ScanResult.Capabilities</c> carries,
/// eg <c>[WPA2-PSK-CCMP][WPS][ESS]</c>.
/// </summary>
/// <remarks>
/// The format is wpa_supplicant's, so the same parser reads it wherever it turns up. It is matched
/// strongest-scheme-first: an access point in a transition mode advertises several, and reporting
/// the weakest of them would tell the caller the network is less secure than it is.
/// </remarks>
internal static class WifiSecurityParser
{
    public static WifiSecurity Parse(string? capabilities)
    {
        if (String.IsNullOrWhiteSpace(capabilities))
            return WifiSecurity.Unknown;

        var caps = capabilities.ToUpperInvariant();

        // SAE is WPA3-Personal; it appears as RSN-SAE, and as WPA2-PSK+SAE in transition mode
        if (caps.Contains("SAE"))
            return WifiSecurity.Wpa3Psk;

        if (caps.Contains("EAP") || caps.Contains("IEEE8021X"))
            return WifiSecurity.Enterprise;

        if (caps.Contains("OWE"))
            return WifiSecurity.Owe;

        // RSN is the 802.11i name for what everyone calls WPA2
        if (caps.Contains("RSN-PSK") || caps.Contains("WPA2-PSK"))
            return WifiSecurity.Wpa2Psk;

        if (caps.Contains("WPA-PSK"))
            return WifiSecurity.WpaPsk;

        if (caps.Contains("WEP"))
            return WifiSecurity.Wep;

        // [ESS] alone means an infrastructure network with no authentication at all
        return caps.Contains("ESS") ? WifiSecurity.Open : WifiSecurity.Unknown;
    }
}
