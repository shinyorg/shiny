namespace Shiny.Bluetooth;


/// <summary>
/// Well-known classic Bluetooth service class UUIDs, for <see cref="IBluetoothDevice.Connect"/> and
/// <see cref="IBluetoothManager.Listen"/>.
/// </summary>
public static class BluetoothServiceUuids
{
    /// <summary>Serial Port Profile (0x1101) - what almost every serial adapter, printer and OBD dongle speaks.</summary>
    public static readonly Guid SerialPort = FromShortId(0x1101);

    /// <summary>Dial-up Networking (0x1103).</summary>
    public static readonly Guid DialupNetworking = FromShortId(0x1103);

    /// <summary>OBEX Object Push (0x1105).</summary>
    public static readonly Guid ObexObjectPush = FromShortId(0x1105);

    /// <summary>OBEX File Transfer (0x1106).</summary>
    public static readonly Guid ObexFileTransfer = FromShortId(0x1106);


    /// <summary>
    /// Expands a 16-bit or 32-bit SIG-assigned id into its full 128-bit UUID on the Bluetooth base
    /// UUID (<c>0000xxxx-0000-1000-8000-00805F9B34FB</c>).
    /// </summary>
    public static Guid FromShortId(uint shortId)
        => new(shortId, 0x0000, 0x1000, 0x80, 0x00, 0x00, 0x80, 0x5F, 0x9B, 0x34, 0xFB);
}
