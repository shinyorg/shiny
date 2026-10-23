namespace Shiny.Net.Vpn;


/// <summary>
/// The IKEv2 VPN configuration an app installs into the OS with <see cref="IVpnManager.SaveProfile"/>.
/// </summary>
/// <remarks>
/// Each app owns exactly one profile - that is how iOS's personal VPN and Android's provisioned VPN
/// profiles both work - so saving again replaces it rather than adding a second.
/// </remarks>
public sealed class VpnProfile
{
    /// <summary>
    /// The name shown in the system VPN settings.
    /// </summary>
    /// <remarks>
    /// Android has nowhere to show it - Settings lists a provisioned profile under the app's own name.
    /// </remarks>
    public required string Name { get; init; }

    /// <summary>The VPN server's host name or IP address.</summary>
    public required string ServerAddress { get; init; }

    /// <summary>How this device proves who it is to the server.</summary>
    public required VpnAuthentication Authentication { get; init; }

    /// <summary>
    /// The identity the server is expected to present (its IKE ID). Defaults to
    /// <see cref="ServerAddress"/>, which is what most servers use.
    /// </summary>
    /// <remarks>
    /// Needs <see cref="VpnCapabilities.RemoteIdentifier"/> to differ from the server address - Android's
    /// profile builder always uses the server address.
    /// </remarks>
    public string? RemoteIdentifier { get; init; }

    /// <summary>
    /// The identity this device presents (its IKE ID).
    /// </summary>
    /// <remarks>
    /// Shared secret servers usually look the secret up by this value. When it is left out, username and
    /// password profiles use the username and certificate profiles use the certificate subject - except
    /// on Android, which has no such fallback and needs it for shared secret and certificate profiles.
    /// </remarks>
    public string? LocalIdentifier { get; init; }

    /// <summary>
    /// The certificate of the CA that issued the server's certificate, DER or PEM encoded. Leave it out
    /// to trust the system's CA store.
    /// </summary>
    /// <remarks>
    /// Needs <see cref="VpnCapabilities.ServerRootCertificate"/>. iOS and macOS only trust a private CA
    /// that the user installed through a configuration profile, so there it cannot be set from code.
    /// </remarks>
    public byte[]? ServerRootCertificate { get; init; }
}


/// <summary>
/// How a <see cref="VpnProfile"/> authenticates. Create one through <see cref="SharedSecret"/>,
/// <see cref="UsernamePassword"/> or <see cref="Certificate"/>.
/// </summary>
/// <remarks>
/// These are classes rather than records on purpose: a record's generated <c>ToString</c> would print the
/// secret into the first log line that formatted one.
/// </remarks>
public abstract class VpnAuthentication
{
    private protected VpnAuthentication() { }

    /// <summary>The <see cref="VpnCapabilities"/> flag a platform needs to save this kind of profile.</summary>
    internal abstract VpnCapabilities RequiredCapability { get; }


    /// <summary>IKEv2 with a pre-shared key.</summary>
    public static VpnAuthentication SharedSecret(string secret)
        => new SharedSecretAuthentication(secret);

    /// <summary>IKEv2 with EAP (EAP-MSCHAPv2) username and password.</summary>
    public static VpnAuthentication UsernamePassword(string username, string password)
        => new UsernamePasswordAuthentication(username, password);

    /// <summary>IKEv2 with a client certificate and its private key, as a PKCS#12 (.p12 / .pfx) bundle.</summary>
    public static VpnAuthentication Certificate(byte[] pkcs12, string? password = null)
        => new CertificateAuthentication(pkcs12, password);
}


/// <summary>IKEv2 with a pre-shared key.</summary>
public sealed class SharedSecretAuthentication : VpnAuthentication
{
    public SharedSecretAuthentication(string secret)
    {
        ArgumentException.ThrowIfNullOrEmpty(secret);
        this.Secret = secret;
    }

    /// <summary>The pre-shared key.</summary>
    public string Secret { get; }

    internal override VpnCapabilities RequiredCapability => VpnCapabilities.SharedSecretAuthentication;
    public override string ToString() => "Shared secret";
}


/// <summary>IKEv2 with EAP (EAP-MSCHAPv2) username and password.</summary>
public sealed class UsernamePasswordAuthentication : VpnAuthentication
{
    public UsernamePasswordAuthentication(string username, string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);
        ArgumentException.ThrowIfNullOrEmpty(password);
        this.Username = username;
        this.Password = password;
    }

    public string Username { get; }
    public string Password { get; }

    internal override VpnCapabilities RequiredCapability => VpnCapabilities.UsernamePasswordAuthentication;
    public override string ToString() => $"Username/password ({this.Username})";
}


/// <summary>IKEv2 with a client certificate and its private key.</summary>
public sealed class CertificateAuthentication : VpnAuthentication
{
    public CertificateAuthentication(byte[] pkcs12, string? password)
    {
        ArgumentNullException.ThrowIfNull(pkcs12);
        if (pkcs12.Length == 0)
            throw new ArgumentException("The PKCS#12 bundle is empty", nameof(pkcs12));

        this.Pkcs12 = pkcs12;
        this.Password = password;
    }

    /// <summary>The PKCS#12 (.p12 / .pfx) bundle holding the client certificate and its private key.</summary>
    public byte[] Pkcs12 { get; }

    /// <summary>The bundle's password, if it has one.</summary>
    public string? Password { get; }

    internal override VpnCapabilities RequiredCapability => VpnCapabilities.CertificateAuthentication;
    public override string ToString() => "Certificate";
}
