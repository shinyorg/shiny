using Shiny.BluetoothLE;

namespace Shiny.Printers.BluetoothLE;


/// <summary>Connects to BLE printers and hands back a ready-to-use <see cref="IPrinter"/>.</summary>
public sealed class BlePrinterManager(IBleManager bleManager)
{
    readonly IBleManager bleManager = bleManager ?? throw new ArgumentNullException(nameof(bleManager));


    /// <summary>Connects to a printer discovered via <see cref="IPrinterScanner"/>.</summary>
    public Task<IPrinter> Connect(DiscoveredPrinter printer, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(printer);
        return this.Connect(printer.Peripheral, printer.Config, cancellationToken, timeout);
    }


    /// <summary>Connects to a known peripheral by UUID using an explicit config (e.g. a custom printer).</summary>
    public Task<IPrinter> Connect(string peripheralUuid, BlePrinterConfig config, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(peripheralUuid);
        var peripheral = this.bleManager.GetKnownPeripheral(peripheralUuid)
            ?? throw new InvalidOperationException($"No known peripheral '{peripheralUuid}'. Scan for it first.");

        return this.Connect(peripheral, config, cancellationToken, timeout);
    }


    /// <summary>Connects to a peripheral with an explicit config and returns a connected printer.</summary>
    public async Task<IPrinter> Connect(IPeripheral peripheral, BlePrinterConfig config, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(peripheral);
        ArgumentNullException.ThrowIfNull(config);

        var connection = new BlePrinterConnection(peripheral, config);
        await connection.Connect(cancellationToken, timeout).ConfigureAwait(false);

        return new Printer(connection, config.ProtocolFactory(), config.Capabilities);
    }
}
