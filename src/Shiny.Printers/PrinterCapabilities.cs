using System.Collections.Generic;

namespace Shiny.Printers;


/// <summary>
/// Static description of what a connected printer can do. This is the "driver constant" surface that
/// user code inspects before building a document - most importantly <see cref="CharactersPerLine"/>
/// and <see cref="DotsPerLine"/>.
/// </summary>
public sealed record PrinterCapabilities
{
    /// <summary>Maximum monospace characters per line in the default font (Font A) at 1x size.</summary>
    public required int CharactersPerLine { get; init; }

    /// <summary>Maximum characters per line in the smaller Font B at 1x size.</summary>
    public int CharactersPerLineFontB { get; init; }

    /// <summary>Printhead resolution in dots-per-inch (almost always 203).</summary>
    public int Dpi { get; init; } = 203;

    /// <summary>Physical printable paper width in millimetres (e.g. 58 or 80).</summary>
    public required double PaperWidthMm { get; init; }

    /// <summary>Number of horizontal dots across the printable area - the max bitmap width.</summary>
    public required int DotsPerLine { get; init; }

    /// <summary>True when the printer can render raster bitmaps.</summary>
    public bool SupportsImages { get; init; } = true;

    /// <summary>True when the printer has an auto-cutter.</summary>
    public bool SupportsCut { get; init; }

    /// <summary>True when the printer can render QR codes.</summary>
    public bool SupportsQrCodes { get; init; } = true;

    /// <summary>The 1D barcode symbologies the printer supports.</summary>
    public IReadOnlySet<BarcodeFormat> SupportedBarcodes { get; init; } = DefaultBarcodes;

    static readonly IReadOnlySet<BarcodeFormat> DefaultBarcodes = new HashSet<BarcodeFormat>
    {
        BarcodeFormat.UpcA, BarcodeFormat.UpcE, BarcodeFormat.Ean13, BarcodeFormat.Ean8,
        BarcodeFormat.Code39, BarcodeFormat.Itf, BarcodeFormat.Codabar,
        BarcodeFormat.Code93, BarcodeFormat.Code128
    };

    /// <summary>Returns true if <paramref name="format"/> is in <see cref="SupportedBarcodes"/>.</summary>
    public bool Supports(BarcodeFormat format) => this.SupportedBarcodes.Contains(format);

    /// <summary>Typical 58mm receipt printer: 384 dots, 32 columns (Font A).</summary>
    public static PrinterCapabilities Paper58mm { get; } = new()
    {
        CharactersPerLine = 32,
        CharactersPerLineFontB = 42,
        PaperWidthMm = 58,
        DotsPerLine = 384,
        SupportsCut = false
    };

    /// <summary>Typical 80mm receipt printer: 576 dots, 48 columns (Font A), usually with a cutter.</summary>
    public static PrinterCapabilities Paper80mm { get; } = new()
    {
        CharactersPerLine = 48,
        CharactersPerLineFontB = 64,
        PaperWidthMm = 80,
        DotsPerLine = 576,
        SupportsCut = true
    };
}
