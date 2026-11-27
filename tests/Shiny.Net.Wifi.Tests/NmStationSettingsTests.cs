using Shiny.Net.Wifi.NetworkManager;
using Xunit;

namespace Shiny.Net.Wifi.Tests;


/// <summary>The NetworkManager profile a join request is turned into.</summary>
public class NmStationSettingsTests
{
    static Dictionary<string, Tmds.DBus.Protocol.VariantValue>? Security(WifiConnectionRequest request)
        => LinuxWifiManager.BuildStationSettings(request).Groups.GetValueOrDefault("802-11-wireless-security");


    [Fact]
    public void APassphraseIsWpaPskByDefault()
    {
        var security = Security(new WifiConnectionRequest("home") { Passphrase = "secret123" })!;
        Assert.Equal("wpa-psk", security["key-mgmt"].GetString());
        Assert.Equal("secret123", security["psk"].GetString());
    }


    [Fact]
    public void Wpa3UsesSae()
    {
        var security = Security(new WifiConnectionRequest("home") { Passphrase = "secret123", Security = WifiSecurity.Wpa3Psk })!;
        Assert.Equal("sae", security["key-mgmt"].GetString());
    }


    /// <remarks>WEP is key-mgmt "none" with the key in wep-key0 - sending it as a WPA psk can never associate.</remarks>
    [Fact]
    public void WepPutsTheKeyInWepKey0()
    {
        var security = Security(new WifiConnectionRequest("old") { Passphrase = "abcde", Security = WifiSecurity.Wep })!;
        Assert.Equal("none", security["key-mgmt"].GetString());
        Assert.Equal("abcde", security["wep-key0"].GetString());
        Assert.False(security.ContainsKey("psk"));
    }


    [Fact]
    public void AnOpenNetworkHasNoSecurityGroup()
        => Assert.Null(Security(new WifiConnectionRequest("cafe")));
}
