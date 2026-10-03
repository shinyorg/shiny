using Shiny.Printers;
using Shiny.Printers.Document;
using Shiny.Printers.Imaging;

namespace Sample.Blazor.Printing;


/// <summary>
/// Builds the demo receipt - the same document the MAUI sample prints over BLE / TCP. Nothing here is
/// browser-specific: <see cref="PrintDocument"/> sits above the transport seam, so the identical builder
/// drives Web Bluetooth, Web Serial and WebUSB.
/// </summary>
public static class SampleReceipt
{
    public const string OrderBarcode = "ORDER-100425";
    public const string LoyaltyUrl = "https://shinylib.net";

    public static PrintDocument Build(PrinterCapabilities caps, PrinterImage? logo = null)
    {
        var doc = new PrintDocument();

        if (logo is not null)
            doc.AlignCenter().Image(logo).Feed();

        doc.AlignCenter()
            .Bold()
            .Size(2, 2)
            .Line("SHINY MART")
            .Size(1, 1)
            .Bold(false)
            .Line("123 Receipt Street")
            .Line("Tel: 555-0100")
            .Feed();

        // Divider sized to the printer's column width - the "driver constant" in action.
        doc.AlignLeft().Line(new string('-', caps.CharactersPerLine));

        doc.Line(Columns("Coffee", "$3.50", caps.CharactersPerLine))
            .Line(Columns("Croissant", "$2.25", caps.CharactersPerLine))
            .Line(Columns("Orange Juice", "$4.00", caps.CharactersPerLine))
            .Line(new string('-', caps.CharactersPerLine));

        doc.Bold()
            .Line(Columns("TOTAL", "$9.75", caps.CharactersPerLine))
            .Bold(false)
            .Feed();

        doc.AlignCenter()
            .Barcode(BarcodeFormat.Code128, OrderBarcode, height: 70, textPosition: BarcodeTextPosition.Below)
            .Feed();

        doc.QrCode(LoyaltyUrl, moduleSize: 6, correction: QrCorrectionLevel.Medium)
            .Feed();

        doc.AlignCenter().Line("Thank you!").Feed(2);

        if (caps.SupportsCut)
            doc.Cut();

        return doc;
    }


    /// <summary>Lays "left ........ right" across the line width.</summary>
    static string Columns(string left, string right, int width)
    {
        var space = width - left.Length - right.Length;
        return space < 1 ? $"{left} {right}" : left + new string(' ', space) + right;
    }
}
