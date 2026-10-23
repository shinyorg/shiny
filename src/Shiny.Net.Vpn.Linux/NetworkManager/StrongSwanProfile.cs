using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Shiny.Net.Vpn.NetworkManager;


/// <summary>
/// Maps a <see cref="VpnProfile"/> onto a NetworkManager connection for the strongSwan plugin
/// (<c>NetworkManager-strongswan</c> / <c>network-manager-strongswan</c>).
/// </summary>
/// <remarks>
/// <para>The plugin is configured through two string maps on the <c>vpn</c> setting: <c>data</c>
/// (address, method, identities, CA file) and <c>secrets</c> (the shared secret or password, always
/// under <c>password</c>). <c>password-flags=0</c> makes NetworkManager store the secret with the
/// profile, so it connects without a secret agent - which a headless or background app does not have.</para>
/// <para>strongSwan was picked over libreswan because it is the plugin that ships IKEv2 EAP and
/// shared-secret support on every mainstream distribution.</para>
/// </remarks>
internal static class StrongSwanProfile
{
    public const string ServiceType = "org.freedesktop.NetworkManager.strongswan";

    static readonly string[] PluginDirectories =
    [
        "/usr/lib/NetworkManager/VPN",
        "/etc/NetworkManager/VPN"
    ];


    public static NmConnectionSettings Build(VpnProfile profile, string uuid, string? serverCaPath)
    {
        var settings = new NmConnectionSettings();

        var connection = settings.Group("connection");
        connection["id"] = profile.Name;
        connection["uuid"] = uuid;
        connection["type"] = "vpn";
        connection["autoconnect"] = false;

        var data = new Dictionary<string, string>
        {
            ["address"] = profile.ServerAddress,

            // ask the server for an inner address - the road-warrior setup every IKEv2 VPN service uses
            ["virtual"] = "yes",
            ["encap"] = "no",
            ["ipcomp"] = "no",
            ["password-flags"] = "0"
        };
        var secrets = new Dictionary<string, string>();

        switch (profile.Authentication)
        {
            case SharedSecretAuthentication psk:
                data["method"] = "psk";
                secrets["password"] = psk.Secret;
                break;

            case UsernamePasswordAuthentication eap:
                data["method"] = "eap";
                data["user"] = eap.Username;
                secrets["password"] = eap.Password;
                break;

            default:
                throw new VpnNotSupportedException($"{profile.Authentication} profiles are not supported by the NetworkManager strongSwan plugin through this library");
        }

        if (profile.LocalIdentifier != null)
            data["local-identity"] = profile.LocalIdentifier;

        if (profile.RemoteIdentifier != null)
            data["remote-identity"] = profile.RemoteIdentifier;

        if (serverCaPath != null)
            data["certificate"] = serverCaPath;

        var vpn = settings.Group("vpn");
        vpn["service-type"] = ServiceType;
        vpn["data"] = data;
        vpn["secrets"] = secrets;

        settings.Group("ipv4")["method"] = "auto";
        settings.Group("ipv6")["method"] = "auto";

        return settings;
    }


    /// <summary>
    /// A stable connection UUID for this app, so the profile is found again on the next launch without
    /// anything being persisted - the same app always owns the same NetworkManager connection.
    /// </summary>
    public static string GetConnectionUuid(string applicationId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("shiny-vpn:" + applicationId));
        var bytes = hash.AsSpan(0, 16).ToArray();

        // stamp it as an RFC 4122 name-based UUID (version 5 layout, variant 1) so it reads as a real one
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes, bigEndian: true).ToString("D");
    }


    /// <summary>
    /// The CA certificate as PEM, which is what strongSwan reads from the path in <c>certificate</c>.
    /// </summary>
    public static string ToPem(byte[] certificate)
    {
        try
        {
            var text = Encoding.ASCII.GetString(certificate);
            using var cert = text.Contains("-----BEGIN CERTIFICATE-----", StringComparison.Ordinal)
                ? X509Certificate2.CreateFromPem(text)
                : X509CertificateLoader.LoadCertificate(certificate);

            return cert.ExportCertificatePem();
        }
        catch (CryptographicException ex)
        {
            throw new ArgumentException("ServerRootCertificate is not a DER or PEM encoded X.509 certificate", nameof(VpnProfile.ServerRootCertificate), ex);
        }
    }


    /// <summary>
    /// Whether the strongSwan plugin is installed, or null when there is no way to tell (no plugin
    /// directory is visible - inside a Flatpak, for example).
    /// </summary>
    /// <remarks>
    /// NetworkManager only notices a missing plugin when the connection is activated, and reports it as a
    /// bare "service start failed". Checking at save time turns that into an error that names the package.
    /// </remarks>
    public static bool? IsPluginInstalled()
    {
        var directories = PluginDirectories.Where(Directory.Exists).ToArray();
        if (directories.Length == 0)
            return null;

        return directories
            .SelectMany(x => Directory.EnumerateFiles(x, "*.name"))
            .Any(x => File.ReadAllText(x).Contains("service=" + ServiceType, StringComparison.Ordinal));
    }


    /// <summary>NM_VPN_CONNECTION_STATE_REASON as a sentence.</summary>
    public static string DescribeReason(uint reason) => reason switch
    {
        2 => "the user disconnected it",
        3 => "the underlying network connection went down",
        4 => "the strongSwan service stopped",
        5 => "the server sent an invalid IP configuration",
        6 => "the server did not respond in time",
        7 => "the strongSwan service did not start in time",
        8 => "the strongSwan service failed to start - check that network-manager-strongswan (and charon-nm) is installed",
        9 => "no secrets were available - save the profile again",
        10 => "the server rejected the login - check the credentials and identities",
        11 => "the profile was removed",
        _ => $"NetworkManager reported reason {reason}"
    };
}
