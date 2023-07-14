using Android.Bluetooth;
using Java.Util;

namespace Shiny.BluetoothLE;


public static class Utils
{
    public static bool Is(this BluetoothGattCharacteristic native, string serviceUuid, string characteristicUuid)
    {
        var nsUuid = ToUuidType(serviceUuid);
        var ncUuid = ToUuidType(characteristicUuid);

        return native.Is(nsUuid, ncUuid);
    }


    public static bool Is(this BluetoothGattCharacteristic native, UUID serviceUuid, UUID characteristicUuid)
    {
        if (!(native?.Service?.Uuid?.Equals(serviceUuid) ?? false))
            return false;

        if (!(native?.Uuid?.Equals(characteristicUuid) ?? false))
            return false;

        return true;
    }

    public static string ToUuidString(string value)
    {
        if (value.Length == 4)
            value = $"0000{value}-0000-1000-8000-00805F9B34FB";

        return value;
    }


    public static UUID ToUuidType(string value)
        => UUID.FromString(Utils.ToUuidString(value));
}
