namespace Shiny.Printing;


/// <summary>
/// Page geometry for <see cref="IPrintService.HtmlToPdf"/>. Sizes are in PDF points (1/72 inch) and describe
/// the paper in portrait; <see cref="Orientation"/> turns it.
/// </summary>
public sealed record PdfPageOptions
{
    /// <summary>Portrait page width in points. Default 595 (A4).</summary>
    public float PageWidth { get; init; } = 595f;

    /// <summary>Portrait page height in points. Default 842 (A4).</summary>
    public float PageHeight { get; init; } = 842f;

    /// <summary><see cref="PrintOrientation.Landscape"/> swaps width and height; anything else is portrait.</summary>
    public PrintOrientation Orientation { get; init; } = PrintOrientation.Portrait;

    /// <summary>
    /// Uniform page margin in points. Default 36 (0.5 inch). Android's WebView also honours a CSS
    /// <c>@page { margin }</c> rule in the document, which wins over this value there.
    /// </summary>
    public float Margin { get; init; } = 36f;

    /// <summary>A4 portrait preset (the default).</summary>
    public static PdfPageOptions A4 { get; } = new();

    /// <summary>US Letter portrait preset (612 x 792 points).</summary>
    public static PdfPageOptions Letter { get; } = new() { PageWidth = 612f, PageHeight = 792f };

    /// <summary>The page width as laid out, after <see cref="Orientation"/>.</summary>
    public float LaidOutWidth => this.Orientation == PrintOrientation.Landscape ? Math.Max(this.PageWidth, this.PageHeight) : Math.Min(this.PageWidth, this.PageHeight);

    /// <summary>The page height as laid out, after <see cref="Orientation"/>.</summary>
    public float LaidOutHeight => this.Orientation == PrintOrientation.Landscape ? Math.Min(this.PageWidth, this.PageHeight) : Math.Max(this.PageWidth, this.PageHeight);
}
