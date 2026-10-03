using Shiny.Printers.Imaging;

namespace Shiny.Printers.Document;


/// <summary>Marker for a single instruction in a <see cref="PrintDocument"/>. Protocols pattern-match on the concrete types.</summary>
public interface IPrintElement;


/// <summary>Sets the horizontal alignment for subsequent output.</summary>
public sealed record AlignElement(PrintAlignment Alignment) : IPrintElement;


/// <summary>Turns emphasised (bold) text on or off.</summary>
public sealed record BoldElement(bool On) : IPrintElement;


/// <summary>Turns underline on or off.</summary>
public sealed record UnderlineElement(bool On) : IPrintElement;


/// <summary>Sets character magnification. <paramref name="WidthScale"/> and <paramref name="HeightScale"/> are 1-8.</summary>
public sealed record SizeElement(int WidthScale, int HeightScale) : IPrintElement;


/// <summary>Resets alignment, emphasis, underline and size back to printer defaults.</summary>
public sealed record ResetStyleElement : IPrintElement;


/// <summary>Prints a run of text. When <see cref="NewLine"/> is true a line-feed is appended.</summary>
public sealed record TextElement(string Text, bool NewLine) : IPrintElement;


/// <summary>Feeds <see cref="Lines"/> blank lines.</summary>
public sealed record FeedElement(int Lines) : IPrintElement;


/// <summary>Prints a 1D barcode.</summary>
public sealed record BarcodeElement(
    BarcodeFormat Format,
    string Data,
    int Height,
    int ModuleWidth,
    BarcodeTextPosition TextPosition
) : IPrintElement;


/// <summary>Prints a QR code.</summary>
public sealed record QrCodeElement(
    string Data,
    int ModuleSize,
    QrCorrectionLevel Correction
) : IPrintElement;


/// <summary>Prints a raster bitmap using the current alignment.</summary>
public sealed record ImageElement(PrinterImage Image) : IPrintElement;


/// <summary>Cuts the paper, optionally feeding <see cref="FeedBefore"/> lines first.</summary>
public sealed record CutElement(CutMode Mode, int FeedBefore) : IPrintElement;


/// <summary>Emits raw, pre-encoded bytes verbatim (escape hatch for printer-specific commands).</summary>
public sealed record RawElement(byte[] Data) : IPrintElement;
