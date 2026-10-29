namespace Shiny.Bluetooth;


/// <summary>
/// Classic Bluetooth (BR/EDR): finds devices, hands out <see cref="IBluetoothDevice"/>s to pair with and
/// connect to, and listens for RFCOMM connections from them.
/// </summary>
/// <remarks>
/// <para>Backed by <c>BluetoothAdapter</c>/<c>BluetoothSocket</c> on Android,
/// <c>Windows.Devices.Bluetooth.Rfcomm</c> on Windows, IOBluetooth on macOS and BlueZ over D-Bus on
/// Linux (via the separate Shiny.Bluetooth.Linux package). iOS and Mac Catalyst have no target:
/// classic Bluetooth there is ExternalAccessory, which only reaches MFi-certified accessories.</para>
/// <para>This is the classic radio only. For Bluetooth Low Energy use Shiny.BluetoothLE and
/// Shiny.BluetoothLE.Hosting.</para>
/// <para>Not every platform can do every operation. Read <see cref="Capabilities"/> before
/// offering a feature; anything unavailable throws <see cref="BluetoothNotSupportedException"/>.</para>
/// </remarks>
public interface IBluetoothManager
{
    /// <summary>
    /// How long <see cref="Discover"/> runs on platforms whose scan has no natural end (Windows, Linux) -
    /// roughly the 12 seconds Android and macOS inquiries take.
    /// </summary>
    static TimeSpan InquiryDuration { get; } = TimeSpan.FromSeconds(12);


    /// <summary>What this platform can actually do. Check before calling.</summary>
    BluetoothCapabilities Capabilities { get; }

    /// <summary>
    /// Asks for whatever the platform needs before the other calls will work, and reports whether
    /// the radio is usable.
    /// </summary>
    /// <remarks>
    /// Android: <c>BLUETOOTH_SCAN</c> + <c>BLUETOOTH_CONNECT</c> on API 31+, <c>BLUETOOTH</c> +
    /// <c>BLUETOOTH_ADMIN</c> + <c>ACCESS_FINE_LOCATION</c> below it. macOS: the Bluetooth privacy
    /// prompt. Windows: radio access. Linux: nothing to ask, so it reports the adapter state.
    /// Returns <see cref="AccessState.Disabled"/> when access is granted but the radio is off.
    /// </remarks>
    Task<AccessState> RequestAccess(CancellationToken ct = default);

    /// <summary>
    /// Lists the classic devices this machine is paired (bonded) with. They do not need to be in range.
    /// </summary>
    Task<IReadOnlyList<IBluetoothDevice>> GetPairedDevices(CancellationToken ct = default);

    /// <summary>
    /// Gets a device by address without discovering it - for a device whose address you already know.
    /// </summary>
    /// <remarks>
    /// Name, pairing state and class are filled in from what the OS already knows about the address; for
    /// an address it has never seen they are empty, and the device can still be connected to on Android,
    /// Windows and macOS. Linux needs BlueZ to know the device (paired, or seen by <see cref="Discover"/>)
    /// before it can connect.
    /// </remarks>
    /// <param name="address"><c>AA:BB:CC:DD:EE:FF</c>, <c>aa-bb-cc-dd-ee-ff</c> or <c>AABBCCDDEEFF</c>.</param>
    /// <param name="ct">Cancels the lookup.</param>
    /// <exception cref="ArgumentException">The address is not a 48-bit Bluetooth address.</exception>
    Task<IBluetoothDevice> GetDevice(string address, CancellationToken ct = default);

    /// <summary>
    /// Runs an inquiry scan and yields each classic device heard, once per address, with the signal
    /// strength it was heard at.
    /// </summary>
    /// <remarks>
    /// The inquiry lasts about 12 seconds - the platform's own cycle on Android and macOS, and
    /// <see cref="InquiryDuration"/> on Windows and Linux, whose scans otherwise run until stopped. Cancel
    /// the token (or stop enumerating) to end it early. Only one inquiry can run at a time, and an
    /// inquiry slows down connections on the same radio considerably, so finish it before calling
    /// <see cref="IBluetoothDevice.Connect"/>.
    /// </remarks>
    /// <exception cref="BluetoothNotSupportedException">The platform cannot run an inquiry.</exception>
    IAsyncEnumerable<BluetoothDiscoveryResult> Discover(CancellationToken ct = default);

    /// <summary>
    /// Publishes an SDP record for <paramref name="serviceUuid"/> and accepts RFCOMM connections to it
    /// from any device.
    /// </summary>
    /// <param name="serviceUuid">The service UUID remote devices will look up and connect to.</param>
    /// <param name="serviceName">The human-readable service name in the SDP record.</param>
    /// <param name="secure">Requires remote devices to authenticate (pair) before connecting.</param>
    /// <param name="ct">Cancels setting up the listener - not the listener itself, which runs until disposed.</param>
    /// <returns>A listener; call <see cref="BluetoothListener.Accept"/> for each incoming connection and dispose
    /// it to withdraw the SDP record.</returns>
    /// <exception cref="BluetoothException">The record could not be published.</exception>
    Task<BluetoothListener> Listen(Guid serviceUuid, string serviceName, bool secure = true, CancellationToken ct = default);

    /// <summary>
    /// Makes this device discoverable by other devices' inquiries for <paramref name="duration"/>.
    /// </summary>
    /// <remarks>
    /// Android asks the user with a system dialog (and needs <c>BLUETOOTH_ADVERTISE</c> on API 31+);
    /// Linux sets the adapter's <c>Discoverable</c> flag. Windows and macOS give apps no way to do
    /// this - a device is discoverable there while its Bluetooth settings page is open.
    /// A device does not need to be discoverable to accept connections from devices it is paired with.
    /// </remarks>
    /// <returns>True when the device is now discoverable, false when the user declined.</returns>
    /// <exception cref="BluetoothNotSupportedException">Windows and macOS.</exception>
    Task<bool> RequestDiscoverable(TimeSpan duration, CancellationToken ct = default);
}
