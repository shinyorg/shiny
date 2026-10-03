using Shiny.Printers.Document;

namespace Shiny.Printers;


/// <summary>
/// Encodes a <see cref="PrintDocument"/> into the byte stream a particular printer command language
/// expects (ESC/POS, TSPL, ZPL, ...). This is the pluggable seam that keeps the document model neutral.
/// </summary>
public interface IPrinterProtocol
{
    /// <summary>A short identifier for the command language (e.g. "ESC/POS").</summary>
    string Name { get; }

    /// <summary>Encodes <paramref name="document"/> for a printer with the given <paramref name="capabilities"/>.</summary>
    byte[] Encode(PrintDocument document, PrinterCapabilities capabilities);
}
