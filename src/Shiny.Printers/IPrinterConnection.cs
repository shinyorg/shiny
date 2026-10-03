namespace Shiny.Printers;


/// <summary>
/// A transport-agnostic byte sink for a printer. Implementations (e.g. Bluetooth LE, USB, TCP) only
/// need to deliver raw command bytes - all protocol/ESC-POS encoding happens above this seam.
/// </summary>
public interface IPrinterConnection
{
    /// <summary>True while the underlying transport is connected and able to accept writes.</summary>
    bool IsConnected { get; }

    /// <summary>Emits whenever the connection state changes. Replays the current state on subscription.</summary>
    IObservable<PrinterConnectionState> WhenStatusChanged();

    /// <summary>
    /// Writes an already-encoded command buffer to the printer. Implementations are responsible for
    /// honouring the transport's maximum payload size (e.g. chunking to the BLE MTU).
    /// </summary>
    ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
}
