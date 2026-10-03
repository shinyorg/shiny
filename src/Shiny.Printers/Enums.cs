namespace Shiny.Printers;


/// <summary>Horizontal alignment of a printed element.</summary>
public enum PrintAlignment
{
    Left = 0,
    Center = 1,
    Right = 2
}


/// <summary>1D barcode symbologies understood by the document model. Not every printer supports every format - check <see cref="PrinterCapabilities.SupportedBarcodes"/>.</summary>
public enum BarcodeFormat
{
    UpcA,
    UpcE,
    Ean13,
    Ean8,
    Code39,
    Itf,
    Codabar,
    Code93,
    Code128
}


/// <summary>Where the human-readable interpretation (HRI) text is printed relative to a barcode.</summary>
public enum BarcodeTextPosition
{
    None = 0,
    Above = 1,
    Below = 2,
    Both = 3
}


/// <summary>QR code error correction level (recovery capacity).</summary>
public enum QrCorrectionLevel
{
    /// <summary>~7% recovery.</summary>
    Low,
    /// <summary>~15% recovery.</summary>
    Medium,
    /// <summary>~25% recovery.</summary>
    Quartile,
    /// <summary>~30% recovery.</summary>
    High
}


/// <summary>Paper cut style.</summary>
public enum CutMode
{
    Full,
    Partial
}


/// <summary>Monochrome conversion strategy used when building a <see cref="Imaging.PrinterImage"/>.</summary>
public enum ImageDithering
{
    /// <summary>Hard threshold - each pixel is black or white based on a luma cutoff. Best for line art / logos.</summary>
    Threshold,
    /// <summary>Floyd-Steinberg error diffusion - approximates greys with dot patterns. Best for photos.</summary>
    FloydSteinberg
}


/// <summary>Connection lifecycle state surfaced by an <see cref="IPrinterConnection"/>.</summary>
public enum PrinterConnectionState
{
    Disconnected,
    Connecting,
    Connected
}
