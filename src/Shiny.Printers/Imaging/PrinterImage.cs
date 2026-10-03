namespace Shiny.Printers.Imaging;


/// <summary>
/// An immutable 1-bit-per-pixel monochrome bitmap ready to be emitted as a raster image. Each row is
/// packed MSB-first into <see cref="Bits"/>; a set bit means a black dot is fired.
/// </summary>
/// <remarks>
/// Decoding PNG/JPEG into pixels is intentionally out of scope for the dependency-free core - obtain
/// RGBA pixels with your platform imaging library (e.g. SkiaSharp) and call <see cref="FromPixels"/>.
/// </remarks>
public sealed class PrinterImage
{
    PrinterImage(int width, int height, byte[] bits)
    {
        this.Width = width;
        this.Height = height;
        this.BytesPerRow = (width + 7) / 8;
        this.Bits = bits;
    }

    /// <summary>Image width in dots.</summary>
    public int Width { get; }

    /// <summary>Image height in dots.</summary>
    public int Height { get; }

    /// <summary>Number of bytes per row (<c>ceil(Width / 8)</c>).</summary>
    public int BytesPerRow { get; }

    /// <summary>Packed monochrome bits, row-major, MSB-first. Length is <c>BytesPerRow * Height</c>.</summary>
    public byte[] Bits { get; }


    /// <summary>Creates an image directly from packed 1bpp data (advanced / testing).</summary>
    public static PrinterImage FromPackedBits(int width, int height, byte[] bits)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        var expected = ((width + 7) / 8) * height;
        if (bits.Length != expected)
            throw new ArgumentException($"Expected {expected} bytes for {width}x{height} 1bpp image but got {bits.Length}.", nameof(bits));

        return new PrinterImage(width, height, bits);
    }


    /// <summary>
    /// Builds a monochrome image from 8-bit grayscale samples (0 = black, 255 = white), one byte per pixel.
    /// </summary>
    public static PrinterImage FromGrayscale(ReadOnlySpan<byte> gray, int width, int height, ImageDithering dithering = ImageDithering.Threshold, byte threshold = 128)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (gray.Length < width * height)
            throw new ArgumentException($"Need at least {width * height} samples for a {width}x{height} image.", nameof(gray));

        // Work on a signed buffer so error-diffusion can carry values out of [0,255].
        var buffer = new int[width * height];
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = gray[i];

        var bytesPerRow = (width + 7) / 8;
        var bits = new byte[bytesPerRow * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var idx = (y * width) + x;
                var old = buffer[idx];
                var isBlack = old < threshold;
                if (isBlack)
                    bits[(y * bytesPerRow) + (x >> 3)] |= (byte)(0x80 >> (x & 7));

                if (dithering == ImageDithering.FloydSteinberg)
                {
                    var newVal = isBlack ? 0 : 255;
                    var error = old - newVal;
                    Diffuse(buffer, width, height, x + 1, y, error * 7 / 16);
                    Diffuse(buffer, width, height, x - 1, y + 1, error * 3 / 16);
                    Diffuse(buffer, width, height, x, y + 1, error * 5 / 16);
                    Diffuse(buffer, width, height, x + 1, y + 1, error * 1 / 16);
                }
            }
        }
        return new PrinterImage(width, height, bits);
    }


    /// <summary>
    /// Builds a monochrome image from 32-bit RGBA pixels (4 bytes per pixel, order R, G, B, A). Luma is
    /// computed with the Rec.601 weights and fully/partly transparent pixels are treated as white.
    /// </summary>
    public static PrinterImage FromPixels(ReadOnlySpan<byte> rgba, int width, int height, ImageDithering dithering = ImageDithering.Threshold, byte threshold = 128)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        var pixelCount = width * height;
        if (rgba.Length < pixelCount * 4)
            throw new ArgumentException($"Need at least {pixelCount * 4} bytes (RGBA) for a {width}x{height} image.", nameof(rgba));

        var gray = new byte[pixelCount];
        for (var i = 0; i < pixelCount; i++)
        {
            var o = i * 4;
            var r = rgba[o];
            var g = rgba[o + 1];
            var b = rgba[o + 2];
            var a = rgba[o + 3];

            // Composite over white using alpha, then convert to luma.
            var luma = ((r * 77) + (g * 150) + (b * 29)) >> 8; // ~0.299/0.587/0.114
            var blended = luma + (((255 - luma) * (255 - a)) / 255);
            gray[i] = (byte)blended;
        }
        return FromGrayscale(gray, width, height, dithering, threshold);
    }


    static void Diffuse(int[] buffer, int width, int height, int x, int y, int delta)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        var idx = (y * width) + x;
        buffer[idx] = Math.Clamp(buffer[idx] + delta, 0, 255);
    }
}
