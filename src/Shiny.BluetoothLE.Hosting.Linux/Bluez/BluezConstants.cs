namespace Shiny.BluetoothLE.Hosting.Bluez;


internal static class BluezConstants
{
    public const string Service = "org.bluez";
    public const string AdapterInterface = "org.bluez.Adapter1";
    public const string GattManagerInterface = "org.bluez.GattManager1";
    public const string GattServiceInterface = "org.bluez.GattService1";
    public const string GattCharacteristicInterface = "org.bluez.GattCharacteristic1";
    public const string GattDescriptorInterface = "org.bluez.GattDescriptor1";
    public const string LEAdvertisingManagerInterface = "org.bluez.LEAdvertisingManager1";
    public const string LEAdvertisementInterface = "org.bluez.LEAdvertisement1";
    public const string ObjectManagerInterface = "org.freedesktop.DBus.ObjectManager";
    public const string PropertiesInterface = "org.freedesktop.DBus.Properties";

    public const string DefaultAdapterPath = "/org/bluez/hci0";
    public const string ApplicationRootPath = "/org/shiny/ble";
    public const string AdvertisementPath = "/org/shiny/ble/advertisement0";
}
