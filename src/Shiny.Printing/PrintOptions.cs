namespace Shiny.Printing;


/// <summary>
/// Per-job options. All properties are optional; the defaults present the system print dialog and let
/// the driver decide layout. Set <see cref="PreferSilent"/> (plus a <see cref="PrinterId"/> on desktop /
/// CUPS) to skip the dialog where the platform allows it.
/// </summary>
public sealed record PrintOptions
{
    /// <summary>Human-readable job name shown in the spooler / print queue.</summary>
    public string? JobName { get; init; }

    /// <summary>
    /// Request printing without showing the OS print UI. Only honoured where
    /// <see cref="PrintingCapabilities.Silent"/> is set (Windows, CUPS, and iOS to a previously
    /// picked printer); ignored on Android and the web, which always present system UI.
    /// </summary>
    public bool PreferSilent { get; init; }

    /// <summary>
    /// Target printer id (as returned by <see cref="PrinterInfo.Id"/>). Required for a meaningful
    /// silent print on desktop / CUPS; when null the platform default printer is used.
    /// </summary>
    public string? PrinterId { get; init; }

    /// <summary>Number of copies. Defaults to 1.</summary>
    public int Copies { get; init; } = 1;

    /// <summary>Requested page orientation.</summary>
    public PrintOrientation Orientation { get; init; } = PrintOrientation.Auto;

    /// <summary>Requested duplex mode.</summary>
    public PrintDuplex Duplex { get; init; } = PrintDuplex.Default;

    /// <summary>Requested colour mode.</summary>
    public PrintColorMode Color { get; init; } = PrintColorMode.Default;
}
