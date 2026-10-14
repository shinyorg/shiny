namespace Shiny.Printers;


/// <summary>An <see cref="IPrinterProtocol"/> whose command language can ask the printer for its status.</summary>
public interface IPrinterStatusProtocol : IPrinterProtocol
{
    /// <summary>Asks the printer for its status over <paramref name="connection"/>.</summary>
    Task<PrinterStatus> QueryStatus(IPrinterQueryConnection connection, CancellationToken cancellationToken = default);
}
