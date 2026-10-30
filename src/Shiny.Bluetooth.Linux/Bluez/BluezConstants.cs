namespace Shiny.Bluetooth.Bluez;


internal static class BluezConstants
{
    public const string Service = "org.bluez";
    public const string RootPath = "/org/bluez";
    public const string AdapterInterface = "org.bluez.Adapter1";
    public const string DeviceInterface = "org.bluez.Device1";
    public const string ProfileManagerInterface = "org.bluez.ProfileManager1";
    public const string ProfileInterface = "org.bluez.Profile1";
    public const string ObjectManagerInterface = "org.freedesktop.DBus.ObjectManager";
    public const string PropertiesInterface = "org.freedesktop.DBus.Properties";
    public const string IntrospectableInterface = "org.freedesktop.DBus.Introspectable";

    public const string DefaultAdapterPath = "/org/bluez/hci0";

    /// <summary>Where this process exports its <c>Profile1</c> objects.</summary>
    public const string ProfileRootPath = "/org/shiny/bluetooth";


    /// <summary>BlueZ's object path for a device - the adapter path plus <c>dev_AA_BB_CC_DD_EE_FF</c>.</summary>
    public static string DevicePath(string adapterPath, string address)
        => $"{adapterPath}/dev_{BluetoothAddress.Normalize(address).Replace(':', '_')}";
}
