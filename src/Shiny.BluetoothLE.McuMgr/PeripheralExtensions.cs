namespace Shiny.BluetoothLE.McuMgr;


/// <summary>
/// Entry points for MCUmgr on a Shiny BLE peripheral.
/// </summary>
public static class McuMgrPeripheralExtensions
{
    /// <summary>
    /// Creates an MCUmgr client that talks to <paramref name="peripheral"/> over the SMP GATT service. The
    /// peripheral is connected on first use if needed. Dispose the client when finished; the connection itself is
    /// left to the app.
    /// </summary>
    public static McuMgrClient CreateMcuMgrClient(
        this IPeripheral peripheral,
        BleSmpTransportOptions? transportOptions = null,
        McuMgrClientOptions? clientOptions = null
    ) => new(new BleSmpTransport(peripheral, transportOptions), clientOptions, ownsTransport: true);


    /// <summary>
    /// Returns true when the peripheral exposes the MCUmgr SMP service. Connects if needed.
    /// </summary>
    public static async Task<bool> HasMcuMgrAsync(this IPeripheral peripheral, CancellationToken cancellationToken = default)
    {
        await peripheral.ConnectAsync(new ConnectionConfig(AutoConnect: false), cancellationToken).ConfigureAwait(false);
        var services = await peripheral.GetServicesAsync(cancellationToken).ConfigureAwait(false);
        return services.Any(x => x.Uuid.Equals(BleSmpTransport.ServiceUuid, StringComparison.OrdinalIgnoreCase));
    }
}
