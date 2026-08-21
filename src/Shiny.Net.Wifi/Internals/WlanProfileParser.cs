using System.Xml.Linq;

namespace Shiny.Net.Wifi.Internals;


/// <summary>
/// Reads what matters out of a Windows <c>WLAN_profile</c> XML document.
/// </summary>
/// <remarks>
/// <para>The only description of a saved profile <c>wlanapi.dll</c> will hand back is this XML, and
/// the parsing is pure - it lives outside the Windows platform folder so it can be tested on any
/// runtime.</para>
/// <para>The schema namespaces every element and its version has moved more than once, so elements
/// are matched on local name only. Anything unparseable comes back as unknown rather than throwing:
/// a profile whose XML cannot be read is still a saved profile.</para>
/// </remarks>
internal static class WlanProfileParser
{
    internal sealed record Result(WifiSecurity Security, bool IsHidden);


    public static Result Parse(string? xml)
    {
        if (String.IsNullOrWhiteSpace(xml))
            return new Result(WifiSecurity.Unknown, false);

        try
        {
            var doc = XDocument.Parse(xml);

            var authentication = doc
                .Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "authentication")?
                .Value;

            var hidden = doc
                .Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "nonBroadcast")?
                .Value;

            return new Result(
                ToSecurity(authentication),
                String.Equals(hidden, "true", StringComparison.OrdinalIgnoreCase)
            );
        }
        catch (System.Xml.XmlException)
        {
            return new Result(WifiSecurity.Unknown, false);
        }
    }


    /// <remarks>
    /// The values the WLAN_profile schema allows in <c>&lt;authentication&gt;</c>. "WPA2" without
    /// the PSK suffix is the enterprise variant - the personal one is always spelled "WPA2PSK" -
    /// which is the one place the naming trips people up.
    /// </remarks>
    public static WifiSecurity ToSecurity(string? authentication) => authentication switch
    {
        "open" => WifiSecurity.Open,
        "shared" => WifiSecurity.Wep,
        "WPA" => WifiSecurity.Enterprise,
        "WPAPSK" => WifiSecurity.WpaPsk,
        "WPA2" => WifiSecurity.Enterprise,
        "WPA2PSK" => WifiSecurity.Wpa2Psk,
        "WPA3" => WifiSecurity.Enterprise,
        "WPA3ENT" => WifiSecurity.Enterprise,
        "WPA3ENT192" => WifiSecurity.Enterprise,
        "WPA3SAE" => WifiSecurity.Wpa3Psk,
        "OWE" => WifiSecurity.Owe,
        _ => WifiSecurity.Unknown
    };
}
