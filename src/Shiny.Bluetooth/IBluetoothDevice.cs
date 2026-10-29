namespace Shiny.Bluetooth;


/// <summary>
/// A remote classic Bluetooth device - from <see cref="IBluetoothManager.Discover"/>,
/// <see cref="IBluetoothManager.GetPairedDevices"/>, <see cref="IBluetoothManager.GetDevice"/> or an
/// accepted <see cref="BluetoothConnection"/>.
/// </summary>
/// <remarks>
/// Two devices are equal when their addresses are, so the same device found by an inquiry and in the
/// paired list compares equal.
/// </remarks>
public interface IBluetoothDevice
{
    /// <summary>The device address, always <c>AA:BB:CC:DD:EE:FF</c> - upper case, colon separated.</summary>
    string Address { get; }

    /// <summary>The device's friendly name, or null when the OS has not resolved it.</summary>
    string? Name { get; }

    /// <summary>Whether this machine is paired (bonded) with the device - as of when this instance was read, or its last <see cref="Pair"/>.</summary>
    bool IsPaired { get; }

    /// <summary>The raw 24-bit Class of Device, when the platform reports it.</summary>
    uint? ClassOfDevice { get; }

    /// <summary>The major device class decoded from <see cref="ClassOfDevice"/>.</summary>
    BluetoothMajorDeviceClass? MajorDeviceClass { get; }

    /// <summary>
    /// Pairs (bonds) with the device. The OS shows its own confirmation or PIN prompt where the pairing
    /// method needs one.
    /// </summary>
    /// <returns>True when the device is paired - including when it already was - and false when the
    /// pairing was refused, failed or timed out.</returns>
    Task<bool> Pair(CancellationToken ct = default);

    /// <summary>
    /// Looks up <paramref name="serviceUuid"/> in the device's SDP records and opens an RFCOMM
    /// connection to it.
    /// </summary>
    /// <param name="serviceUuid">The service to connect to, such as <see cref="BluetoothServiceUuids.SerialPort"/>.</param>
    /// <param name="secure">Requires an authenticated, encrypted link - which pairs on the way in if the
    /// devices are not already paired. Pass false for devices that cannot pair (Android's
    /// "insecure" sockets, Windows <c>PlainSocket</c>). macOS always lets the system decide.</param>
    /// <param name="ct">Cancels the connection attempt.</param>
    /// <returns>An open connection; dispose it to disconnect.</returns>
    /// <exception cref="BluetoothConnectionException">The service was not found or the connection failed.</exception>
    Task<BluetoothConnection> Connect(Guid serviceUuid, bool secure = true, CancellationToken ct = default);
}


/// <summary>
/// The major device class bits (8-12) of a Class of Device.
/// </summary>
public enum BluetoothMajorDeviceClass
{
    Miscellaneous = 0x00,
    Computer = 0x01,
    Phone = 0x02,
    NetworkAccessPoint = 0x03,
    AudioVideo = 0x04,
    Peripheral = 0x05,
    Imaging = 0x06,
    Wearable = 0x07,
    Toy = 0x08,
    Health = 0x09,
    Uncategorized = 0x1F
}
