using Shiny.Printers;
using Shiny.Printers.Document;
using Shiny.Printers.Imaging;
using SkiaSharp;

namespace Sample.Shared.Maui.Pages.Printing;


/// <summary>
/// The demo receipt both printing pages print - styled text, alignment, a barcode, a QR code, a logo and a cut.
/// The thermal page streams it as ESC/POS; the native page renders the very same document to a PDF.
/// </summary>
public static class SampleReceipt
{
    public const string OrderBarcode = "ORDER-100425";
    public const string LoyaltyUrl = "https://shinylib.net";

    // an embedded resource rather than a MauiAsset - MAUI's FileSystem is not implemented on the macOS head
    const string LogoResource = "Sample.Shared.Maui.Pages.Printing.receipt-logo.png";


    public static PrintDocument Build(PrinterCapabilities caps)
    {
        var doc = new PrintDocument();

        var logo = LoadLogo(caps);
        if (logo != null)
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
            .Feed()
            .QrCode(LoyaltyUrl, moduleSize: 6, correction: QrCorrectionLevel.Medium)
            .Feed()
            .Line("Thank you!")
            .Feed(2);

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


    // Shiny.Printers never decodes PNG/JPEG - that is the caller's job. SkiaSharp decodes to RGBA here and
    // PrinterImage.FromPixels does the monochrome (Floyd-Steinberg) conversion.
    static PrinterImage? LoadLogo(PrinterCapabilities caps)
    {
        if (!caps.SupportsImages)
            return null;

        using var stream = typeof(SampleReceipt).Assembly.GetManifestResourceStream(LogoResource);
        if (stream == null)
            return null;

        using var codec = SKCodec.Create(stream);
        if (codec == null)
            return null;

        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var bitmap = new SKBitmap(info);
        if (codec.GetPixels(info, bitmap.GetPixels()) != SKCodecResult.Success)
            return null;

        // a raster wider than the printhead does not print at all
        if (bitmap.Width > caps.DotsPerLine)
            return null;

        return PrinterImage.FromPixels(bitmap.GetPixelSpan(), bitmap.Width, bitmap.Height, ImageDithering.FloydSteinberg);
    }
}
