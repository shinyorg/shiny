namespace Shiny.Printing;


/// <summary>Page orientation requested for a print job. <see cref="Auto"/> lets the platform/driver decide.</summary>
public enum PrintOrientation
{
    Auto = 0,
    Portrait = 1,
    Landscape = 2
}


/// <summary>Duplex (two-sided) preference. <see cref="Default"/> honours the driver/printer default.</summary>
public enum PrintDuplex
{
    Default = 0,
    OneSided = 1,
    TwoSidedLongEdge = 2,
    TwoSidedShortEdge = 3
}


/// <summary>Colour preference for a job. <see cref="Default"/> honours the driver/printer default.</summary>
public enum PrintColorMode
{
    Default = 0,
    Color = 1,
    Monochrome = 2
}


/// <summary>Outcome of a <see cref="IPrintService.Print"/> call.</summary>
public enum PrintStatus
{
    /// <summary>The job finished / was fully spooled and (where observable) accepted.</summary>
    Completed,

    /// <summary>The user dismissed the system print UI without printing.</summary>
    Cancelled,

    /// <summary>The job could not be submitted (see <see cref="PrintResult.Error"/>).</summary>
    Failed,

    /// <summary>The job was handed to the OS spooler but its final state isn't observable from here.</summary>
    Submitted
}


/// <summary>The concrete content type carried by a <see cref="PrintJob"/>. Drives per-platform routing.</summary>
public enum PrintContentKind
{
    Pdf,
    Image,
    Html,
    HtmlUrl,
    File
}


/// <summary>
/// What the current platform's <see cref="IPrintService"/> supports. Callers should feature-detect
/// against <see cref="IPrintService.Capabilities"/> rather than catch <see cref="PlatformNotSupportedException"/>.
/// </summary>
[Flags]
public enum PrintingCapabilities
{
    None = 0,

    /// <summary>Can print PDF documents.</summary>
    Pdf = 1,

    /// <summary>Can print raster images (PNG / JPEG).</summary>
    Image = 2,

    /// <summary>Can print HTML markup / a web URL.</summary>
    Html = 4,

    /// <summary>Can print an arbitrary file, routed by extension.</summary>
    File = 8,

    /// <summary>Presents the OS print dialog / sheet so the user picks a printer.</summary>
    SystemDialog = 16,

    /// <summary>Can print silently (no UI) to a named or default printer.</summary>
    Silent = 32,

    /// <summary>Can enumerate installed printers via <see cref="IPrintService.GetPrinters"/>.</summary>
    EnumeratePrinters = 64
}
