namespace Shiny.Printers.Network;


/// <summary>Connects to network printers over TCP and hands back a ready-to-use <see cref="IPrinter"/>.</summary>
public sealed class NetworkPrinterManager
{
    /// <summary>Connects using a full <see cref="NetworkPrinterConfig"/>.</summary>
    public async Task<IPrinter> Connect(NetworkPrinterConfig config, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(config);

        var connection = new TcpPrinterConnection(config);
        await connection.Connect(cancellationToken, timeout).ConfigureAwait(false);

        return new Printer(connection, config.ProtocolFactory(), config.Capabilities);
    }


    /// <summary>Connects to <paramref name="host"/>:<paramref name="port"/> with the given capabilities.</summary>
    public Task<IPrinter> Connect(string host, int port, PrinterCapabilities capabilities, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
        => this.Connect(
            new NetworkPrinterConfig { Host = host, Port = port, Capabilities = capabilities },
            cancellationToken,
            timeout
        );


    /// <summary>Connects to a printer discovered via <see cref="INetworkPrinterScanner"/>.</summary>
    public Task<IPrinter> Connect(DiscoveredNetworkPrinter printer, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(printer);
        return this.Connect(
            new NetworkPrinterConfig { Host = printer.Host, Port = printer.Port, Capabilities = printer.Capabilities },
            cancellationToken,
            timeout
        );
    }
}
