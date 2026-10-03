using Shiny.Printers.Document;

namespace Shiny.Printers;


/// <summary>A connected printer: knows its <see cref="Capabilities"/> and can print documents or raw bytes.</summary>
/// <remarks>
/// The BLE and network managers return a <see cref="Printer"/> - dispose it to disconnect. The Blazor
/// transports return a <c>BrowserPrinter</c>, which is <see cref="IAsyncDisposable"/>.
/// </remarks>
public interface IPrinter
{
    /// <summary>What this printer can do - inspect before building a document.</summary>
    PrinterCapabilities Capabilities { get; }

    /// <summary>True while the underlying connection is live.</summary>
    bool IsConnected { get; }

    /// <summary>Encodes and sends a document to the printer.</summary>
    Task Print(PrintDocument document, CancellationToken cancellationToken = default);

    /// <summary>Sends already-encoded bytes to the printer verbatim.</summary>
    Task PrintRaw(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
}
