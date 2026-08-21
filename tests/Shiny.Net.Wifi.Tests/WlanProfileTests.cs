using Shiny.Net.Wifi.Internals;
using Xunit;

namespace Shiny.Net.Wifi.Tests;


/// <summary>
/// The Windows saved-profile store describes a profile only as XML, so everything
/// <see cref="IWifiManager.GetKnownNetworks"/> reports on Windows comes out of this parser.
/// </summary>
public class WlanProfileTests
{
    static string Profile(string authentication, string encryption = "AES", bool hidden = false) =>
        $"""
        <?xml version="1.0"?>
        <WLANProfile xmlns="http://www.microsoft.com/networking/WLAN/profile/v1">
            <name>TestNet</name>
            <SSIDConfig>
                <SSID><name>TestNet</name></SSID>
                <nonBroadcast>{(hidden ? "true" : "false")}</nonBroadcast>
            </SSIDConfig>
            <connectionType>ESS</connectionType>
            <MSM>
                <security>
                    <authEncryption>
                        <authentication>{authentication}</authentication>
                        <encryption>{encryption}</encryption>
                        <useOneX>false</useOneX>
                    </authEncryption>
                </security>
            </MSM>
        </WLANProfile>
        """;


    [Theory]
    [InlineData("open", WifiSecurity.Open)]
    [InlineData("shared", WifiSecurity.Wep)]
    [InlineData("WPAPSK", WifiSecurity.WpaPsk)]
    [InlineData("WPA2PSK", WifiSecurity.Wpa2Psk)]
    [InlineData("WPA3SAE", WifiSecurity.Wpa3Psk)]
    [InlineData("OWE", WifiSecurity.Owe)]
    public void SchemeIsReadFromTheAuthenticationElement(string authentication, WifiSecurity expected)
        => Assert.Equal(expected, WlanProfileParser.Parse(Profile(authentication)).Security);


    /// <remarks>
    /// The suffix-less spellings are the enterprise variants - the personal ones always carry PSK
    /// or SAE - and reporting one of those as personal would understate what the network needs.
    /// </remarks>
    [Theory]
    [InlineData("WPA")]
    [InlineData("WPA2")]
    [InlineData("WPA3ENT")]
    [InlineData("WPA3ENT192")]
    public void SuffixlessSpellingsAreEnterprise(string authentication)
        => Assert.Equal(WifiSecurity.Enterprise, WlanProfileParser.Parse(Profile(authentication)).Security);


    [Fact]
    public void NonBroadcastIsAHiddenNetwork()
        => Assert.True(WlanProfileParser.Parse(Profile("WPA2PSK", hidden: true)).IsHidden);


    [Fact]
    public void BroadcastingProfilesAreNotHidden()
        => Assert.False(WlanProfileParser.Parse(Profile("WPA2PSK")).IsHidden);


    /// <remarks>
    /// The profile schema has been through more than one namespace, so nothing may be matched on a
    /// fully qualified name.
    /// </remarks>
    [Fact]
    public void AnUnexpectedNamespaceStillParses()
    {
        var xml = Profile("WPA2PSK").Replace(
            "http://www.microsoft.com/networking/WLAN/profile/v1",
            "http://www.microsoft.com/networking/WLAN/profile/v9"
        );
        Assert.Equal(WifiSecurity.Wpa2Psk, WlanProfileParser.Parse(xml).Security);
    }


    /// <remarks>
    /// A profile that cannot be read is still a saved profile - it has to come back as unknown
    /// rather than throwing, or one bad entry would take the whole listing down.
    /// </remarks>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<WLANProfile>")]
    [InlineData("not xml at all")]
    public void UnreadableProfilesAreUnknownRatherThanFatal(string? xml)
    {
        var result = WlanProfileParser.Parse(xml);
        Assert.Equal(WifiSecurity.Unknown, result.Security);
        Assert.False(result.IsHidden);
    }


    [Fact]
    public void AnUnrecognisedSchemeIsUnknown()
        => Assert.Equal(WifiSecurity.Unknown, WlanProfileParser.Parse(Profile("WPA4TELEPATHY")).Security);
}
