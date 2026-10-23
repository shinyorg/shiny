namespace Shiny.Net.Vpn.NetworkManager;


internal static class NmConstants
{
    public const string Service = "org.freedesktop.NetworkManager";
    public const string RootPath = "/org/freedesktop/NetworkManager";
    public const string SettingsPath = "/org/freedesktop/NetworkManager/Settings";

    public const string ManagerInterface = "org.freedesktop.NetworkManager";
    public const string SettingsInterface = "org.freedesktop.NetworkManager.Settings";
    public const string SettingsConnectionInterface = "org.freedesktop.NetworkManager.Settings.Connection";
    public const string ActiveConnectionInterface = "org.freedesktop.NetworkManager.Connection.Active";
    public const string VpnConnectionInterface = "org.freedesktop.NetworkManager.VPN.Connection";
    public const string PropertiesInterface = "org.freedesktop.DBus.Properties";

    public const string InvalidConnectionError = "org.freedesktop.NetworkManager.Settings.InvalidConnection";
    public const string SettingsPermissionDeniedError = "org.freedesktop.NetworkManager.Settings.PermissionDenied";
    public const string PermissionDeniedError = "org.freedesktop.NetworkManager.PermissionDenied";

    /// <summary>The "no object" object path NetworkManager uses in place of null.</summary>
    public const string NullPath = "/";

    /// <summary>NM_ACTIVE_CONNECTION_STATE_*</summary>
    public const uint ActiveStateActivating = 1;
    public const uint ActiveStateActivated = 2;
    public const uint ActiveStateDeactivating = 3;
    public const uint ActiveStateDeactivated = 4;

    /// <summary>NM_VPN_CONNECTION_STATE_FAILED</summary>
    public const uint VpnStateFailed = 6;
}
