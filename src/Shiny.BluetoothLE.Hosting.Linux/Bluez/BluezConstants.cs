namespace Shiny.BluetoothLE.Hosting.Bluez;


internal static class BluezConstants
{
    public const string Service = "org.bluez";
    public const string AdapterInterface = "org.bluez.Adapter1";
    public const string DeviceInterface = "org.bluez.Device1";
    public const string GattManagerInterface = "org.bluez.GattManager1";
    public const string GattServiceInterface = "org.bluez.GattService1";
    public const string GattCharacteristicInterface = "org.bluez.GattCharacteristic1";
    public const string GattDescriptorInterface = "org.bluez.GattDescriptor1";
    public const string LEAdvertisingManagerInterface = "org.bluez.LEAdvertisingManager1";
    public const string LEAdvertisementInterface = "org.bluez.LEAdvertisement1";
    public const string ObjectManagerInterface = "org.freedesktop.DBus.ObjectManager";
    public const string IntrospectableInterface = "org.freedesktop.DBus.Introspectable";
    public const string PropertiesInterface = "org.freedesktop.DBus.Properties";

    public const string DefaultAdapterPath = "/org/bluez/hci0";

    // kept apart from the advertisement paths - the GATT application root is an ObjectManager and
    // everything beneath it is read by BlueZ as part of the application
    public const string ApplicationRootPath = "/org/shiny/ble/gatt";
    public const string AdvertisementPathPrefix = "/org/shiny/ble/advertisement";

    public const string ErrorFailed = "org.bluez.Error.Failed";
    public const string ErrorNotPermitted = "org.bluez.Error.NotPermitted";
    public const string ErrorNotAuthorized = "org.bluez.Error.NotAuthorized";
    public const string ErrorNotSupported = "org.bluez.Error.NotSupported";
    public const string ErrorInvalidOffset = "org.bluez.Error.InvalidOffset";
    public const string ErrorInvalidValueLength = "org.bluez.Error.InvalidValueLength";
}
