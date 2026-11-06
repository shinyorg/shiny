namespace Shiny.BluetoothLE.McuMgr;


/// <summary>
/// Carries SMP frames to and from a device. <see cref="BleSmpTransport"/> is the Bluetooth LE implementation;
/// implement this for serial, UDP or a test double.
/// </summary>
/// <remarks>
/// <see cref="McuMgrClient"/> sends one request at a time, so implementations do not need to handle concurrent
/// <see cref="SendAsync"/> calls.
/// </remarks>
public interface ISmpTransport
{
    /// <summary>
    /// The largest SMP frame (header + payload) the transport can deliver to the device in a single unit - for BLE
    /// the negotiated MTU payload size. Frames larger than this may still be sent when the device can reassemble
    /// them (see <see cref="McuMgrOs.GetParametersAsync"/>).
    /// </summary>
    int MaxFrameSize { get; }

    /// <summary>
    /// Sends one complete SMP frame and returns the complete response frame whose header sequence number matches
    /// the request's (byte 6). Responses with another sequence number - a late reply to a request that already
    /// timed out - must be discarded.
    /// </summary>
    Task<byte[]> SendAsync(byte[] frame, CancellationToken cancellationToken);

    /// <summary>
    /// Called after the device has been told to reset. Waits for the link to drop, waits
    /// <paramref name="delay"/> (time for the bootloader to swap images), then reconnects.
    /// </summary>
    Task ReconnectAsync(TimeSpan delay, TimeSpan timeout, CancellationToken cancellationToken);
}
