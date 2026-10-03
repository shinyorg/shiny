using Shiny.Printers.Document;

namespace Shiny.Printers;


/// <summary>
/// Default <see cref="IPrinter"/>: composes a transport (<see cref="IPrinterConnection"/>), a command
/// language (<see cref="IPrinterProtocol"/>) and a static capability set.
/// </summary>
/// <remarks>
/// Disposing the printer disposes its <see cref="Connection"/> when the transport is <see cref="IDisposable"/>
/// (the BLE and TCP connections are), which disconnects and releases it.
/// </remarks>
public sealed class Printer(IPrinterConnection connection, IPrinterProtocol protocol, PrinterCapabilities capabilities) : IPrinter, IDisposable
{
    readonly IPrinterConnection connection = connection ?? throw new ArgumentNullException(nameof(connection));
    readonly IPrinterProtocol protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));

    /// <summary>The transport this printer writes to.</summary>
    public IPrinterConnection Connection => this.connection;

    /// <summary>The command language this printer encodes documents with.</summary>
    public IPrinterProtocol Protocol => this.protocol;

    public PrinterCapabilities Capabilities { get; } = capabilities ?? throw new ArgumentNullException(nameof(capabilities));

    public bool IsConnected => this.connection.IsConnected;

    public Task Print(PrintDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        var bytes = this.protocol.Encode(document, this.Capabilities);
        return this.connection.SendAsync(bytes, cancellationToken).AsTask();
    }

    public Task PrintRaw(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        => this.connection.SendAsync(data, cancellationToken).AsTask();

    /// <summary>Disconnects and releases the underlying <see cref="Connection"/>.</summary>
    public void Dispose() => (this.connection as IDisposable)?.Dispose();
}
