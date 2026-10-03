using Shiny.Printers.Document;
using Shiny.Printers.Imaging;
using Shiny.Printing.Rendering;

namespace Shiny.Printers.Tests;


public class PrintDocumentRenderingTests
{
    static readonly IPrintDocumentRenderer Renderer = new SkiaPrintDocumentRenderer();

    static bool IsPdf(byte[] data)
        => data.Length > 4 && data[0] == '%' && data[1] == 'P' && data[2] == 'D' && data[3] == 'F';

    [Fact]
    public void Renders_Simple_Receipt_To_Pdf()
    {
        var doc = new PrintDocument()
            .AlignCenter().Bold().Line("SHINY MART").ResetStyle()
            .AlignLeft().Line("Coffee            $3.50")
            .Line("Muffin            $2.25")
            .Feed(2);

        var pdf = Renderer.RenderToPdf(doc);

        Assert.True(IsPdf(pdf));
        Assert.True(pdf.Length > 400);
    }

    [Fact]
    public void Handles_Barcode_Qr_And_Image_Without_Throwing()
    {
        var image = PrinterImage.FromPackedBits(8, 2, [0b10101010, 0b01010101]);
        var doc = new PrintDocument()
            .Barcode(BarcodeFormat.Code128, "ORDER-1")
            .QrCode("https://shiny.dev")
            .AlignCenter().Image(image);

        var pdf = Renderer.RenderToPdf(doc);

        Assert.True(IsPdf(pdf));
    }

    [Fact]
    public void Long_Document_Paginates_Beyond_One_Page()
    {
        var doc = new PrintDocument();
        for (var i = 0; i < 400; i++)
            doc.Line($"Line {i}");

        // A4 at the default font holds far fewer than 400 lines, so more content must mean more bytes.
        var many = Renderer.RenderToPdf(doc);
        var few = Renderer.RenderToPdf(new PrintDocument().Line("Line 0"));

        Assert.True(IsPdf(many));
        Assert.True(many.Length > few.Length);
    }

    [Fact]
    public void Letter_Preset_Renders()
    {
        var pdf = Renderer.RenderToPdf(new PrintDocument().Line("Hello"), PrintRenderOptions.Letter);

        Assert.True(IsPdf(pdf));
    }
}
