namespace Shiny.Printing.Rendering;


/// <summary>
/// Page geometry and typography for rendering a <c>PrintDocument</c> to PDF. Defaults approximate an
/// A4 portrait page with a monospace font, which suits receipt-style documents printed to office paper.
/// </summary>
public sealed record PrintRenderOptions
{
    /// <summary>Page width in PDF points (1/72 inch). Default 595 (A4).</summary>
    public float PageWidth { get; init; } = 595f;

    /// <summary>Page height in PDF points. Default 842 (A4).</summary>
    public float PageHeight { get; init; } = 842f;

    /// <summary>Uniform page margin in points. Default 36 (0.5 inch).</summary>
    public float Margin { get; init; } = 36f;

    /// <summary>Base font size in points at 1x magnification. Default 12.</summary>
    public float FontSize { get; init; } = 12f;

    /// <summary>Extra leading between lines as a multiple of font size. Default 0.35.</summary>
    public float LineSpacing { get; init; } = 0.35f;

    /// <summary>Font family name. Default a monospace face for aligned receipts.</summary>
    public string FontFamily { get; init; } = "monospace";

    /// <summary>A4 portrait preset (the default).</summary>
    public static PrintRenderOptions A4 { get; } = new();

    /// <summary>US Letter portrait preset (612 x 792 points).</summary>
    public static PrintRenderOptions Letter { get; } = new() { PageWidth = 612f, PageHeight = 792f };
}
