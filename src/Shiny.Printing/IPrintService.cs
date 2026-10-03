namespace Shiny.Printing;


/// <summary>
/// Prints documents through the operating system's own print pipeline (spooler + driver + printer
/// picker), so any installed printer - AirPrint, laser/inkjet, CUPS - can be targeted. This is distinct
/// from the thermal <c>Shiny.Printers</c> stack, which streams raw ESC/POS / TSPL bytes to a transport.
/// </summary>
public interface IPrintService
{
    /// <summary>What this platform's implementation supports - feature-detect before building a job.</summary>
    PrintingCapabilities Capabilities { get; }

    /// <summary>
    /// Submits a job. By default the OS print dialog / sheet is shown; set
    /// <see cref="PrintOptions.PreferSilent"/> to print without UI where
    /// <see cref="PrintingCapabilities.Silent"/> is supported.
    /// </summary>
    Task<PrintResult> Print(PrintJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates installed printers. Returns an empty list where
    /// <see cref="PrintingCapabilities.EnumeratePrinters"/> is not supported (iOS, Android, web).
    /// </summary>
    Task<IReadOnlyList<PrinterInfo>> GetPrinters(CancellationToken cancellationToken = default);
}
