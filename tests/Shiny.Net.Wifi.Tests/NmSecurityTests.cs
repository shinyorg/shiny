using Shiny.Net.Wifi.NetworkManager;
using Xunit;

namespace Shiny.Net.Wifi.Tests;


/// <summary>
/// NetworkManager reports security as three separate bitfields rather than a name, so the mapping
/// back to <see cref="WifiSecurity"/> is all inference and worth pinning down.
/// </summary>
public class NmSecurityTests
{
    [Fact]
    public void NoFlagsIsAnOpenNetwork()
        => Assert.Equal(
            WifiSecurity.Open,
            NmClient.ToSecurity(NmApFlags.None, NmApSecurity.None, NmApSecurity.None)
        );


    [Fact]
    public void PrivacyWithoutWpaOrRsnIsWep()
        => Assert.Equal(
            WifiSecurity.Wep,
            NmClient.ToSecurity(NmApFlags.Privacy, NmApSecurity.None, NmApSecurity.None)
        );


    [Fact]
    public void WpaOnlyPskIsWpa1()
        => Assert.Equal(
            WifiSecurity.WpaPsk,
            NmClient.ToSecurity(NmApFlags.Privacy, NmApSecurity.KeyMgmtPsk | NmApSecurity.PairTkip, NmApSecurity.None)
        );


    [Fact]
    public void RsnPskIsWpa2()
        => Assert.Equal(
            WifiSecurity.Wpa2Psk,
            NmClient.ToSecurity(NmApFlags.Privacy, NmApSecurity.None, NmApSecurity.KeyMgmtPsk | NmApSecurity.PairCcmp)
        );


    [Fact]
    public void SaeIsWpa3()
        => Assert.Equal(
            WifiSecurity.Wpa3Psk,
            NmClient.ToSecurity(NmApFlags.Privacy, NmApSecurity.None, NmApSecurity.KeyMgmtSae)
        );


    [Fact]
    public void TransitionModeReportsTheStrongerScheme()
    {
        // a WPA2/WPA3 transition AP sets both PSK and SAE in the RSN element
        Assert.Equal(
            WifiSecurity.Wpa3Psk,
            NmClient.ToSecurity(NmApFlags.Privacy, NmApSecurity.KeyMgmtPsk, NmApSecurity.KeyMgmtPsk | NmApSecurity.KeyMgmtSae)
        );
    }


    [Fact]
    public void EnterpriseBeatsPsk()
        => Assert.Equal(
            WifiSecurity.Enterprise,
            NmClient.ToSecurity(NmApFlags.Privacy, NmApSecurity.None, NmApSecurity.KeyMgmtPsk | NmApSecurity.KeyMgmt8021X)
        );


    [Fact]
    public void SuiteBIsEnterprise()
        => Assert.Equal(
            WifiSecurity.Enterprise,
            NmClient.ToSecurity(NmApFlags.Privacy, NmApSecurity.None, NmApSecurity.KeyMgmtEapSuiteB192)
        );


    [Fact]
    public void OweIsRecognised()
    {
        Assert.Equal(WifiSecurity.Owe, NmClient.ToSecurity(NmApFlags.None, NmApSecurity.None, NmApSecurity.KeyMgmtOwe));
        Assert.Equal(WifiSecurity.Owe, NmClient.ToSecurity(NmApFlags.None, NmApSecurity.None, NmApSecurity.KeyMgmtOweTransition));
    }
}
