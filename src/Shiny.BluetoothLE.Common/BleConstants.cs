namespace Shiny.BluetoothLE;


/// <summary>
/// Values fixed by the Bluetooth core specification that are needed to convert between an ATT MTU and the
/// payload size Shiny surfaces on <c>IPeripheral.Mtu</c>.
/// </summary>
public static class BleConstants
{
    /// <summary>
    /// The size, in bytes, of the ATT header that precedes the payload of every GATT operation. Subtract this
    /// from a negotiated ATT MTU to get the number of bytes that actually fit in a single operation.
    /// </summary>
    public const int AttHeaderSize = 3;

    /// <summary>
    /// The ATT MTU (23 bytes) that every BLE link starts with before any negotiation takes place.
    /// </summary>
    public const int DefaultAttMtu = 23;

    /// <summary>
    /// The usable payload size (20 bytes) on a link running at the default ATT MTU. This is the value
    /// <c>IPeripheral.Mtu</c> reports until a larger MTU is negotiated.
    /// </summary>
    public const int DefaultPayloadSize = DefaultAttMtu - AttHeaderSize;
}
