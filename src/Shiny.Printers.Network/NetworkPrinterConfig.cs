using Shiny.Printers.Protocols.EscPos;

namespace Shiny.Printers.Network;


/// <summary>
/// Everything needed to talk to a network (WiFi / Ethernet) printer over a raw TCP socket: the
/// <see cref="Host"/> / <see cref="Port"/>, the static capabilities and the command language.
/// </summary>
public sealed record NetworkPrinterConfig
{
    /// <summary>The printer's host name or IP address.</summary>
    public required string Host { get; init; }

    /// <summary>The TCP port. Defaults to 9100 - the de-facto "RAW" / JetDirect / AppSocket port.</summary>
    public int Port { get; init; } = 9100;

    /// <summary>How long to wait for the TCP connect before giving up. Default 10s.</summary>
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Static capabilities for this printer (chars-per-line, paper width, etc).</summary>
    public required PrinterCapabilities Capabilities { get; init; }

    /// <summary>Factory for the command-language encoder. Defaults to <see cref="EscPosProtocol"/>.</summary>
    public Func<IPrinterProtocol> ProtocolFactory { get; init; } = static () => new EscPosProtocol();
}
