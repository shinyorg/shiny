using System.Collections.Generic;
using Shiny.Printers.Imaging;

namespace Shiny.Printers.Document;


/// <summary>
/// A protocol-neutral, fluent description of what to print. Build it up with the chainable methods and
/// hand it to <see cref="IPrinter.Print"/>; the printer's <see cref="IPrinterProtocol"/> turns it into bytes.
/// </summary>
public sealed class PrintDocument
{
    readonly List<IPrintElement> elements = new();

    /// <summary>The ordered elements that make up the document.</summary>
    public IReadOnlyList<IPrintElement> Elements => this.elements;

    /// <summary>Appends an element directly. Most callers use the typed helpers below instead.</summary>
    public PrintDocument Add(IPrintElement element)
    {
        this.elements.Add(element);
        return this;
    }

    // ---- styling -------------------------------------------------------------------------------

    /// <summary>Sets horizontal alignment for subsequent content.</summary>
    public PrintDocument Align(PrintAlignment alignment) => this.Add(new AlignElement(alignment));

    /// <summary>Convenience for <c>Align(Left)</c>.</summary>
    public PrintDocument AlignLeft() => this.Align(PrintAlignment.Left);

    /// <summary>Convenience for <c>Align(Center)</c>.</summary>
    public PrintDocument AlignCenter() => this.Align(PrintAlignment.Center);

    /// <summary>Convenience for <c>Align(Right)</c>.</summary>
    public PrintDocument AlignRight() => this.Align(PrintAlignment.Right);

    /// <summary>Turns bold/emphasis on or off.</summary>
    public PrintDocument Bold(bool on = true) => this.Add(new BoldElement(on));

    /// <summary>Turns underline on or off.</summary>
    public PrintDocument Underline(bool on = true) => this.Add(new UnderlineElement(on));

    /// <summary>Sets character magnification (1-8 in each axis).</summary>
    public PrintDocument Size(int widthScale, int heightScale)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(widthScale, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(widthScale, 8);
        ArgumentOutOfRangeException.ThrowIfLessThan(heightScale, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(heightScale, 8);
        return this.Add(new SizeElement(widthScale, heightScale));
    }

    /// <summary>Sets both width and height magnification to the same value.</summary>
    public PrintDocument Size(int scale) => this.Size(scale, scale);

    /// <summary>Resets alignment, emphasis, underline and size to defaults.</summary>
    public PrintDocument ResetStyle() => this.Add(new ResetStyleElement());

    // ---- text ----------------------------------------------------------------------------------

    /// <summary>Prints text with no trailing line-feed.</summary>
    public PrintDocument Text(string text) => this.Add(new TextElement(text, NewLine: false));

    /// <summary>Prints text followed by a line-feed.</summary>
    public PrintDocument Line(string text = "") => this.Add(new TextElement(text, NewLine: true));

    /// <summary>Word-wraps <paramref name="text"/> to <paramref name="charactersPerLine"/> columns and prints each line.</summary>
    public PrintDocument WrapText(string text, int charactersPerLine)
    {
        foreach (var line in TextWrap.Wrap(text, charactersPerLine))
            this.Line(line);
        return this;
    }

    /// <summary>Feeds <paramref name="lines"/> blank lines (default 1).</summary>
    public PrintDocument Feed(int lines = 1) => this.Add(new FeedElement(lines));

    // ---- graphics ------------------------------------------------------------------------------

    /// <summary>Prints a 1D barcode.</summary>
    public PrintDocument Barcode(
        BarcodeFormat format,
        string data,
        int height = 80,
        int moduleWidth = 2,
        BarcodeTextPosition textPosition = BarcodeTextPosition.Below
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(data);
        return this.Add(new BarcodeElement(format, data, height, moduleWidth, textPosition));
    }

    /// <summary>Prints a QR code.</summary>
    public PrintDocument QrCode(
        string data,
        int moduleSize = 6,
        QrCorrectionLevel correction = QrCorrectionLevel.Medium
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(data);
        ArgumentOutOfRangeException.ThrowIfLessThan(moduleSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(moduleSize, 16);
        return this.Add(new QrCodeElement(data, moduleSize, correction));
    }

    /// <summary>Prints a raster bitmap using the current alignment.</summary>
    public PrintDocument Image(PrinterImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        return this.Add(new ImageElement(image));
    }

    // ---- control -------------------------------------------------------------------------------

    /// <summary>Cuts the paper.</summary>
    public PrintDocument Cut(CutMode mode = CutMode.Partial, int feedBefore = 3) => this.Add(new CutElement(mode, feedBefore));

    /// <summary>Emits raw, pre-encoded bytes verbatim.</summary>
    public PrintDocument Raw(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return this.Add(new RawElement(data));
    }
}
