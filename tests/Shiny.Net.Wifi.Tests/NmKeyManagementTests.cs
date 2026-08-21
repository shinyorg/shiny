using Shiny.Net.Wifi.NetworkManager;
using Xunit;

namespace Shiny.Net.Wifi.Tests;


/// <summary>
/// A saved NetworkManager profile names its key management outright rather than advertising cipher
/// bits, so this mapping is exact where the beacon-derived one is inference.
/// </summary>
public class NmKeyManagementTests
{
    /// <remarks>
    /// No security group on the profile at all - which is how NetworkManager stores an open network.
    /// </remarks>
    [Fact]
    public void AMissingKeyManagementIsAnOpenNetwork()
        => Assert.Equal(WifiSecurity.Open, NmClient.ToSecurity(null));


    /// <remarks>
    /// NetworkManager's spelling of WEP: key management really is "none", with the key in wep-key0
    /// rather than psk. Reporting it as open would call an encrypted network unencrypted.
    /// </remarks>
    [Fact]
    public void NoneIsWepRatherThanOpen()
        => Assert.Equal(WifiSecurity.Wep, NmClient.ToSecurity("none"));


    [Theory]
    [InlineData("wpa-psk", WifiSecurity.Wpa2Psk)]
    [InlineData("sae", WifiSecurity.Wpa3Psk)]
    [InlineData("owe", WifiSecurity.Owe)]
    [InlineData("wpa-eap", WifiSecurity.Enterprise)]
    [InlineData("wpa-eap-suite-b-192", WifiSecurity.Enterprise)]
    [InlineData("ieee8021x", WifiSecurity.Enterprise)]
    public void KnownKeyManagementsMap(string keyManagement, WifiSecurity expected)
        => Assert.Equal(expected, NmClient.ToSecurity(keyManagement));


    [Fact]
    public void AnUnrecognisedKeyManagementIsUnknown()
        => Assert.Equal(WifiSecurity.Unknown, NmClient.ToSecurity("wpa-psk-sha384"));
}
