using CoreBluetooth;

namespace Shiny.BluetoothLE;


internal static class PlatformExtensions
{
    public static bool Is(this CBCharacteristic native, string serviceUuid, string characteristicUuid)
    {
        var nsUuid = CBUUID.FromString(serviceUuid);
        var ncUuid = CBUUID.FromString(characteristicUuid);

        return native.Is(nsUuid, ncUuid);
    }


    public static bool Is(this CBCharacteristic native, CBUUID serviceUuid, CBUUID characteristicUuid)
    {
        if (!(native?.Service?.UUID?.Equals(serviceUuid) ?? false))
            return false;

        if (!(native?.UUID?.Equals(characteristicUuid) ?? false))
            return false;

        return true;
    }

#if XAMARIN
    public static bool IsUnknown(this CBCentralManagerState state)
        => state == CBCentralManagerState.Unknown;


    public static AccessState FromNative(this CBCentralManagerState state) => state switch
    {
        CBCentralManagerState.Resetting => AccessState.Available,
        CBCentralManagerState.PoweredOn => AccessState.Available,
        CBCentralManagerState.PoweredOff => AccessState.Disabled,
        CBCentralManagerState.Unauthorized => AccessState.Denied,
        CBCentralManagerState.Unsupported => AccessState.NotSupported,
        _ => AccessState.Unknown
    };
#else
    public static bool IsUnknown(this CBManagerState state)
        => state == CBManagerState.Unknown;


    public static AccessState FromNative(this CBManagerState state) => state switch
    {
        CBManagerState.Resetting => AccessState.Available,
        CBManagerState.PoweredOn => AccessState.Available,
        CBManagerState.PoweredOff => AccessState.Disabled,
        CBManagerState.Unauthorized => AccessState.Denied,
        CBManagerState.Unsupported => AccessState.NotSupported,
        _ => AccessState.Unknown
    };
#endif
}
