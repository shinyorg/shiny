namespace Shiny.Printing;


/// <summary>A printer discovered by <see cref="IPrintService.GetPrinters"/>.</summary>
/// <param name="Id">Opaque platform id - pass back via <see cref="PrintOptions.PrinterId"/> to target it.</param>
/// <param name="DisplayName">Human-friendly name for UI.</param>
/// <param name="IsDefault">True if this is the system default printer.</param>
public sealed record PrinterInfo(string Id, string DisplayName, bool IsDefault);
