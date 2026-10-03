using Shiny.Printers.Document;

namespace Shiny.Printing.Rendering;


/// <summary>
/// Renders a fluent <see cref="PrintDocument"/> (the thermal document model) into a PDF that can be
/// handed to <c>IPrintService.Print(PrintJob.Pdf(...))</c> and printed on any OS-native printer. This
/// bridges the <c>Shiny.Printers</c> document model to <c>Shiny.Printing</c> without a per-platform
/// layout engine.
/// </summary>
public interface IPrintDocumentRenderer
{
    /// <summary>Renders the document to a PDF byte buffer.</summary>
    byte[] RenderToPdf(PrintDocument document, PrintRenderOptions? options = null);
}
