using Shiny.Printers;
using Shiny.Printers.Imaging;

namespace Shiny.Printers.Tests;


public class PrinterImageTests
{
    [Fact]
    public void FromPackedBits_Exposes_Geometry()
    {
        var img = PrinterImage.FromPackedBits(width: 10, height: 2, bits: new byte[2 * 2]);
        Assert.Equal(10, img.Width);
        Assert.Equal(2, img.Height);
        Assert.Equal(2, img.BytesPerRow); // ceil(10/8) = 2
    }

    [Fact]
    public void FromPackedBits_Validates_Length()
        => Assert.Throws<ArgumentException>(() => PrinterImage.FromPackedBits(8, 1, new byte[2]));

    [Fact]
    public void Grayscale_Threshold_Sets_MSB_First()
    {
        // 8 px alternating black/white -> 0b10101010 = 0xAA
        byte[] gray = [0, 255, 0, 255, 0, 255, 0, 255];
        var img = PrinterImage.FromGrayscale(gray, 8, 1, ImageDithering.Threshold, threshold: 128);
        Assert.Equal(0xAA, img.Bits[0]);
    }

    [Fact]
    public void Grayscale_Black_Pixel_Is_Set()
    {
        var img = PrinterImage.FromGrayscale([0], 1, 1, ImageDithering.Threshold);
        Assert.Equal(0x80, img.Bits[0]);
    }

    [Fact]
    public void Grayscale_White_Pixel_Is_Clear()
    {
        var img = PrinterImage.FromGrayscale([255], 1, 1, ImageDithering.Threshold);
        Assert.Equal(0x00, img.Bits[0]);
    }

    [Fact]
    public void FromPixels_Treats_Transparent_As_White()
    {
        // Fully transparent black should NOT fire a dot.
        byte[] rgba = [0, 0, 0, 0];
        var img = PrinterImage.FromPixels(rgba, 1, 1, ImageDithering.Threshold);
        Assert.Equal(0x00, img.Bits[0]);
    }

    [Fact]
    public void FromPixels_Opaque_Black_Fires_Dot()
    {
        byte[] rgba = [0, 0, 0, 255];
        var img = PrinterImage.FromPixels(rgba, 1, 1, ImageDithering.Threshold);
        Assert.Equal(0x80, img.Bits[0]);
    }
}
