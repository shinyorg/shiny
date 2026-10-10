using Shiny.Printers.Document;

namespace Shiny.Printers.Blazor;


/// <summary>
/// A connected browser-attached printer. Behaves exactly like any other <see cref="IPrinter"/> - the
/// same <see cref="PrintDocument"/> and protocol encoders as the BLE and TCP transports - but also owns
/// its <see cref="Connection"/>, so dispose it (or call <see cref="BrowserPrinterConnection.Disconnect"/>)
/// to give the device back to the browser.
/// </summary>
public sealed class BrowserPrinter : IPrinter, IAsyncDisposable
{
    readonly Printer printer;

    internal BrowserPrinter(BrowserPrinterConnection connection, IPrinterProtocol protocol, PrinterCapabilities capabilities)
    {
        this.Connection = connection;
        this.printer = new Printer(connection, protocol, capabilities);
    }

    /// <summary>The underlying browser transport - device name, id and connection state changes.</summary>
    public BrowserPrinterConnection Connection { get; }

    public PrinterCapabilities Capabilities => this.printer.Capabilities;

    public bool IsConnected => this.printer.IsConnected;

    public Task Print(PrintDocument document, CancellationToken cancellationToken = default)
        => this.printer.Print(document, cancellationToken);

    public Task PrintRaw(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        => this.printer.PrintRaw(data, cancellationToken);

    public ValueTask DisposeAsync() => this.Connection.DisposeAsync();
}
