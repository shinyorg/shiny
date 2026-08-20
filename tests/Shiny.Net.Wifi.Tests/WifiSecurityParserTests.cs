using Shiny.Net.Wifi.Internals;
using Xunit;

namespace Shiny.Net.Wifi.Tests;


public class WifiSecurityParserTests
{
    [Theory]
    [InlineData("[ESS]", WifiSecurity.Open)]
    [InlineData("[WEP][ESS]", WifiSecurity.Wep)]
    [InlineData("[WPA-PSK-TKIP][ESS]", WifiSecurity.WpaPsk)]
    [InlineData("[WPA2-PSK-CCMP][WPS][ESS]", WifiSecurity.Wpa2Psk)]
    [InlineData("[RSN-PSK-CCMP][ESS]", WifiSecurity.Wpa2Psk)]
    [InlineData("[RSN-SAE-CCMP][ESS]", WifiSecurity.Wpa3Psk)]
    [InlineData("[RSN-OWE-CCMP][ESS]", WifiSecurity.Owe)]
    [InlineData("[RSN-EAP-CCMP][ESS]", WifiSecurity.Enterprise)]
    [InlineData("[WPA2-EAP-CCMP][ESS]", WifiSecurity.Enterprise)]
    public void CapabilityStringsAreParsed(string capabilities, WifiSecurity expected)
        => Assert.Equal(expected, WifiSecurityParser.Parse(capabilities));


    [Fact]
    public void TransitionModeReportsTheStrongerScheme()
    {
        // an access point in WPA2/WPA3 transition advertises both; reporting the WPA2 half would
        // tell the caller the network is weaker than it is
        Assert.Equal(WifiSecurity.Wpa3Psk, WifiSecurityParser.Parse("[RSN-PSK+SAE-CCMP][ESS]"));
        Assert.Equal(WifiSecurity.Owe, WifiSecurityParser.Parse("[RSN-OWE_TRANSITION-CCMP][ESS]"));
    }


    [Fact]
    public void EnterpriseBeatsPsk()
    {
        // a network offering both a PSK and 802.1X is an enterprise network with a guest fallback
        Assert.Equal(WifiSecurity.Enterprise, WifiSecurityParser.Parse("[RSN-PSK+EAP-CCMP][ESS]"));
    }


    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingCapabilitiesAreUnknown(string? capabilities)
        => Assert.Equal(WifiSecurity.Unknown, WifiSecurityParser.Parse(capabilities));


    [Fact]
    public void ParsingIsCaseInsensitive()
        => Assert.Equal(WifiSecurity.Wpa2Psk, WifiSecurityParser.Parse("[wpa2-psk-ccmp][ess]"));
}
