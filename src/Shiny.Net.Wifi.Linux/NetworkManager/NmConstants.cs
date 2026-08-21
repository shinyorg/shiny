namespace Shiny.Net.Wifi.NetworkManager;


internal static class NmConstants
{
    public const string Service = "org.freedesktop.NetworkManager";
    public const string RootPath = "/org/freedesktop/NetworkManager";

    public const string ManagerInterface = "org.freedesktop.NetworkManager";
    public const string DeviceInterface = "org.freedesktop.NetworkManager.Device";
    public const string WirelessInterface = "org.freedesktop.NetworkManager.Device.Wireless";
    public const string AccessPointInterface = "org.freedesktop.NetworkManager.AccessPoint";
    public const string Ip4ConfigInterface = "org.freedesktop.NetworkManager.IP4Config";
    public const string ActiveConnectionInterface = "org.freedesktop.NetworkManager.Connection.Active";
    public const string PropertiesInterface = "org.freedesktop.DBus.Properties";
    public const string SettingsInterface = "org.freedesktop.NetworkManager.Settings";
    public const string SettingsConnectionInterface = "org.freedesktop.NetworkManager.Settings.Connection";

    public const string SettingsPath = "/org/freedesktop/NetworkManager/Settings";

    /// <summary>NM_DEVICE_TYPE_WIFI</summary>
    public const uint DeviceTypeWifi = 2;

    /// <summary>NM_DEVICE_STATE_ACTIVATED</summary>
    public const uint DeviceStateActivated = 100;

    /// <summary>The "no object" object path NetworkManager uses in place of null.</summary>
    public const string NullPath = "/";
}


/// <summary>
/// NM_802_11_AP_FLAGS - what the beacon itself advertises.
/// </summary>
[Flags]
internal enum NmApFlags : uint
{
    None = 0,
    Privacy = 0x1,
    Wps = 0x2
}


/// <summary>
/// NM_802_11_AP_SEC - the cipher and key-management bits carried separately for WPA and RSN.
/// </summary>
[Flags]
internal enum NmApSecurity : uint
{
    None = 0,
    PairWep40 = 0x1,
    PairWep104 = 0x2,
    PairTkip = 0x4,
    PairCcmp = 0x8,
    GroupWep40 = 0x10,
    GroupWep104 = 0x20,
    GroupTkip = 0x40,
    GroupCcmp = 0x80,
    KeyMgmtPsk = 0x100,
    KeyMgmt8021X = 0x200,
    KeyMgmtSae = 0x400,
    KeyMgmtOwe = 0x800,
    KeyMgmtOweTransition = 0x1000,
    KeyMgmtEapSuiteB192 = 0x2000
}
