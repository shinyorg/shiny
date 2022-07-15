namespace Shiny.BluetoothLE;

public enum GattCharacteristicResultType
{
    Read,
    Write,
    WriteWithoutResponse,
    Notification
}


public record GattCharacteristicResult(
    IGattCharacteristic Characteristic,
    byte[]? Data,
    GattCharacteristicResultType Type
);