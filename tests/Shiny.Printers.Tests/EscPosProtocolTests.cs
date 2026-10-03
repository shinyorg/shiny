using System;
using Shiny.Printers;
using Shiny.Printers.Document;
using Shiny.Printers.Imaging;
using Shiny.Printers.Protocols.EscPos;

namespace Shiny.Printers.Tests;


public class EscPosProtocolTests
{
    static readonly PrinterCapabilities Caps = PrinterCapabilities.Paper58mm;

    // ESC @  +  ESC t 0
    static readonly byte[] Header = [0x1B, 0x40, 0x1B, 0x74, 0x00];

    static byte[] Encode(Action<PrintDocument> build)
    {
        var doc = new PrintDocument();
        build(doc);
        return new EscPosProtocol().Encode(doc, Caps);
    }

    /// <summary>Returns just the bytes emitted after the fixed init header.</summary>
    static byte[] Body(byte[] full) => full[Header.Length..];


    [Fact]
    public void Emits_Init_And_CodePage_Header()
    {
        var bytes = Encode(_ => { });
        Assert.Equal(Header, bytes);
    }

    [Theory]
    [InlineData(PrintAlignment.Left, 0)]
    [InlineData(PrintAlignment.Center, 1)]
    [InlineData(PrintAlignment.Right, 2)]
    public void Align_Emits_ESC_a_n(PrintAlignment alignment, byte n)
    {
        var body = Body(Encode(d => d.Align(alignment)));
        Assert.Equal([0x1B, 0x61, n], body);
    }

    [Fact]
    public void Bold_On_Off()
    {
        Assert.Equal([0x1B, 0x45, 1], Body(Encode(d => d.Bold())));
        Assert.Equal([0x1B, 0x45, 0], Body(Encode(d => d.Bold(false))));
    }

    [Fact]
    public void Underline_On() => Assert.Equal([0x1B, 0x2D, 1], Body(Encode(d => d.Underline())));

    [Fact]
    public void Size_Packs_Width_High_Nibble_Height_Low_Nibble()
    {
        // width 2 -> (2-1)=1 in high nibble; height 3 -> (3-1)=2 in low nibble => 0x12
        Assert.Equal([0x1D, 0x21, 0x12], Body(Encode(d => d.Size(2, 3))));
    }

    [Fact]
    public void Text_With_NewLine_Appends_LF()
    {
        var body = Body(Encode(d => d.Line("Hi")));
        Assert.Equal([(byte)'H', (byte)'i', 0x0A], body);
    }

    [Fact]
    public void Text_Without_NewLine_Has_No_LF()
    {
        var body = Body(Encode(d => d.Text("Hi")));
        Assert.Equal([(byte)'H', (byte)'i'], body);
    }

    [Fact]
    public void Feed_Emits_ESC_d_n() => Assert.Equal([0x1B, 0x64, 2], Body(Encode(d => d.Feed(2))));

    [Fact]
    public void Cut_Partial_With_Feed()
    {
        var body = Body(Encode(d => d.Cut(CutMode.Partial, feedBefore: 3)));
        Assert.Equal([0x1B, 0x64, 3, 0x1D, 0x56, 1], body);
    }

    [Fact]
    public void Cut_Full_No_Feed()
    {
        var body = Body(Encode(d => d.Cut(CutMode.Full, feedBefore: 0)));
        Assert.Equal([0x1D, 0x56, 0], body);
    }

    [Fact]
    public void ResetStyle_Reissues_Init()
    {
        var body = Body(Encode(d => d.ResetStyle()));
        Assert.Equal([0x1B, 0x40, 0x1B, 0x74, 0x00], body);
    }

    [Fact]
    public void Barcode_Code128_Frames_With_Selector_73_And_CodeSetB()
    {
        var body = Body(Encode(d => d.Barcode(BarcodeFormat.Code128, "123", height: 80, moduleWidth: 2, textPosition: BarcodeTextPosition.Below)));
        Assert.Equal(
        [
            0x1D, 0x48, 0x02,             // GS H 2 (HRI below)
            0x1D, 0x68, 0x50,             // GS h 80 (height)
            0x1D, 0x77, 0x02,             // GS w 2 (module width)
            0x1D, 0x6B, 0x49, 0x05,       // GS k 73, length 5
            (byte)'{', (byte)'B', (byte)'1', (byte)'2', (byte)'3'
        ], body);
    }

    [Fact]
    public void Barcode_Ean13_Uses_Selector_67()
    {
        var body = Body(Encode(d => d.Barcode(BarcodeFormat.Ean13, "12")));
        // ... GS k 67 02 '1' '2'
        Assert.Equal([0x1D, 0x6B, 0x43, 0x02, (byte)'1', (byte)'2'], body[9..]);
    }

    [Fact]
    public void QrCode_Emits_Model_Size_Ec_Store_Print_Sequences()
    {
        var body = Body(Encode(d => d.QrCode("AB", moduleSize: 6, correction: QrCorrectionLevel.Medium)));
        Assert.Equal(
        [
            0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00, // model 2
            0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x06,       // module size 6
            0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x31,       // EC level M (49)
            0x1D, 0x28, 0x6B, 0x05, 0x00, 0x31, 0x50, 0x30, (byte)'A', (byte)'B', // store (len = 2+3)
            0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30        // print
        ], body);
    }

    [Fact]
    public void Image_Emits_GS_v0_Raster_Header_And_Bits()
    {
        var image = PrinterImage.FromPackedBits(8, 1, [0xFF]);
        var body = Body(Encode(d => d.Image(image)));
        Assert.Equal(
        [
            0x1D, 0x76, 0x30, 0x00, // GS v 0 m
            0x01, 0x00,             // xL xH (bytes per row = 1)
            0x01, 0x00,             // yL yH (height = 1)
            0xFF                    // the single row of bits
        ], body);
    }

    [Fact]
    public void Raw_Passes_Through_Verbatim()
    {
        var body = Body(Encode(d => d.Raw([0xDE, 0xAD, 0xBE, 0xEF])));
        Assert.Equal([0xDE, 0xAD, 0xBE, 0xEF], body);
    }
}
