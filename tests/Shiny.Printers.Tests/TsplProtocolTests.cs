using System.Text;
using Shiny.Printers.Document;
using Shiny.Printers.Protocols.Tspl;

namespace Shiny.Printers.Tests;


public class TsplProtocolTests
{
    static string Encode(PrintDocument doc)
        => Encoding.ASCII.GetString(new TsplProtocol().Encode(doc, PrinterCapabilities.Paper80mm));


    /// <remarks>TSPL has no backslash escape - a quote inside a string must be written \["] or it ends the string.</remarks>
    [Fact]
    public void Quotes_In_Text_Use_The_Tspl_Escape()
    {
        var tspl = Encode(new PrintDocument().Line("12\" pizza"));

        Assert.Contains("\"12\\[\"] pizza\"", tspl);
        Assert.DoesNotContain("\\\"", tspl);
    }


    [Fact]
    public void Quotes_In_Barcode_And_Qr_Data_Use_The_Tspl_Escape()
    {
        var tspl = Encode(new PrintDocument().Barcode(BarcodeFormat.Code128, "A\"B").QrCode("say \"hi\""));

        Assert.Contains("\"A\\[\"]B\"", tspl);
        Assert.Contains("\"say \\[\"]hi\\[\"]\"", tspl);
    }
}
