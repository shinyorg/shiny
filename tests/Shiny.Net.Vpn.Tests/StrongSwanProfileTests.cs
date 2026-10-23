using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Shiny.Net.Vpn.NetworkManager;
using Xunit;

namespace Shiny.Net.Vpn.Tests;


/// <summary>
/// The NetworkManager connection a VpnProfile becomes. A wrong key here compiles, saves without
/// complaint and only fails when strongSwan reads it - so the mapping is pinned down.
/// </summary>
public class StrongSwanProfileTests
{
    const string Uuid = "8d3b1c2e-0000-5000-8000-000000000000";


    static Dictionary<string, string> Data(NmConnectionSettings settings)
        => (Dictionary<string, string>)settings.Groups["vpn"]["data"];

    static Dictionary<string, string> Secrets(NmConnectionSettings settings)
        => (Dictionary<string, string>)settings.Groups["vpn"]["secrets"];


    [Fact]
    public void ASharedSecretProfileUsesThePskMethod()
    {
        var settings = StrongSwanProfile.Build(new VpnProfile
        {
            Name = "Office",
            ServerAddress = "vpn.example.com",
            LocalIdentifier = "client@example.com",
            Authentication = VpnAuthentication.SharedSecret("a-very-long-pre-shared-key")
        }, Uuid, null);

        Assert.Equal("vpn", settings.Groups["connection"]["type"]);
        Assert.Equal(Uuid, settings.Groups["connection"]["uuid"]);
        Assert.Equal("Office", settings.Groups["connection"]["id"]);
        Assert.Equal(false, settings.Groups["connection"]["autoconnect"]);
        Assert.Equal(StrongSwanProfile.ServiceType, settings.Groups["vpn"]["service-type"]);

        var data = Data(settings);
        Assert.Equal("vpn.example.com", data["address"]);
        Assert.Equal("psk", data["method"]);
        Assert.Equal("client@example.com", data["local-identity"]);
        Assert.Equal("yes", data["virtual"]);
        Assert.False(data.ContainsKey("user"));
        Assert.False(data.ContainsKey("remote-identity"));
        Assert.False(data.ContainsKey("certificate"));

        Assert.Equal("a-very-long-pre-shared-key", Secrets(settings)["password"]);
    }


    /// <remarks>
    /// password-flags=0 stores the secret with the profile. Anything else makes NetworkManager ask a
    /// secret agent at connect time, which a background or headless app does not have.
    /// </remarks>
    [Fact]
    public void TheSecretIsStoredWithTheProfile()
    {
        var settings = StrongSwanProfile.Build(new VpnProfile
        {
            Name = "Office",
            ServerAddress = "vpn.example.com",
            Authentication = VpnAuthentication.UsernamePassword("allan", "pw")
        }, Uuid, null);

        Assert.Equal("0", Data(settings)["password-flags"]);
    }


    [Fact]
    public void AUsernamePasswordProfileUsesEap()
    {
        var settings = StrongSwanProfile.Build(new VpnProfile
        {
            Name = "Office",
            ServerAddress = "vpn.example.com",
            RemoteIdentifier = "server.example.com",
            Authentication = VpnAuthentication.UsernamePassword("allan", "pw")
        }, Uuid, "/home/u/.local/share/shiny/vpn/ca.pem");

        var data = Data(settings);
        Assert.Equal("eap", data["method"]);
        Assert.Equal("allan", data["user"]);
        Assert.Equal("server.example.com", data["remote-identity"]);
        Assert.Equal("/home/u/.local/share/shiny/vpn/ca.pem", data["certificate"]);
        Assert.Equal("pw", Secrets(settings)["password"]);
    }


    [Fact]
    public void ACertificateProfileIsRefused()
        => Assert.Throws<VpnNotSupportedException>(() => StrongSwanProfile.Build(new VpnProfile
        {
            Name = "Office",
            ServerAddress = "vpn.example.com",
            Authentication = VpnAuthentication.Certificate([1, 2, 3])
        }, Uuid, null));


    /// <remarks>
    /// The UUID is how the app finds its profile on the next launch - it must not change between runs,
    /// and two apps must not land on the same connection.
    /// </remarks>
    [Fact]
    public void TheConnectionUuidIsStablePerApp()
    {
        var first = StrongSwanProfile.GetConnectionUuid("MyApp");

        Assert.Equal(first, StrongSwanProfile.GetConnectionUuid("MyApp"));
        Assert.NotEqual(first, StrongSwanProfile.GetConnectionUuid("OtherApp"));
        Assert.True(Guid.TryParse(first, out _));
        Assert.Equal('5', first[14]);
    }


    [Fact]
    public void ADerCertificateIsWrittenAsPem()
    {
        var der = CreateCertificate().Export(X509ContentType.Cert);
        var pem = StrongSwanProfile.ToPem(der);

        Assert.StartsWith("-----BEGIN CERTIFICATE-----", pem);
    }


    [Fact]
    public void APemCertificatePassesThrough()
    {
        var pem = CreateCertificate().ExportCertificatePem();
        Assert.Equal(pem, StrongSwanProfile.ToPem(System.Text.Encoding.ASCII.GetBytes(pem)));
    }


    [Fact]
    public void GarbageIsNotACertificate()
        => Assert.Throws<ArgumentException>(() => StrongSwanProfile.ToPem([1, 2, 3, 4]));


    [Theory]
    [InlineData(10u, "rejected the login")]
    [InlineData(8u, "network-manager-strongswan")]
    [InlineData(999u, "999")]
    public void FailureReasonsAreDescribed(uint reason, string expected)
        => Assert.Contains(expected, StrongSwanProfile.DescribeReason(reason));


    static X509Certificate2 CreateCertificate()
    {
        using var key = ECDsa.Create();
        var request = new CertificateRequest("CN=Test CA", key, HashAlgorithmName.SHA256);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }
}
