using Foundation;
using Security;

namespace Shiny.Net.Vpn;


/// <summary>
/// The keychain items behind a personal VPN's secrets.
/// </summary>
/// <remarks>
/// NEVPNProtocol takes its shared secret and password as keychain <em>persistent references</em>, not
/// values - the VPN agent reads the item itself when it negotiates. So the secret is stored as a
/// generic password and its persistent reference is what goes into the protocol.
/// </remarks>
static class AppleVpnKeychain
{
    public const string SharedSecretAccount = "shared-secret";
    public const string PasswordAccount = "password";

    static string Service => (NSBundle.MainBundle.BundleIdentifier ?? "shiny") + ".shiny.vpn";


    public static NSData Store(string account, string secret)
    {
        Delete(account);

        var record = new SecRecord(SecKind.GenericPassword)
        {
            Service = Service,
            Account = account,
            ValueData = NSData.FromString(secret, NSStringEncoding.UTF8),

            // the tunnel can reconnect while the device is locked, so the item must stay readable then
            Accessible = SecAccessible.AfterFirstUnlockThisDeviceOnly
        };

        var status = SecKeyChain.Add(record);
        if (status != SecStatusCode.Success)
            throw new VpnException($"The VPN secret could not be written to the keychain ({status})");

        var reference = SecKeyChain.QueryAsData(Query(account), true, out status);
        if (status != SecStatusCode.Success || reference == null)
            throw new VpnException($"The VPN secret's keychain reference could not be read back ({status})");

        return reference;
    }


    public static void Delete(string account)
        => SecKeyChain.Remove(Query(account));


    static SecRecord Query(string account) => new(SecKind.GenericPassword)
    {
        Service = Service,
        Account = account
    };
}
