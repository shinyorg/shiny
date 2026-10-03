using Shiny.Printers.Protocols.EscPos;

namespace Shiny.Printers.BluetoothLE;


/// <summary>
/// Everything needed to talk to a specific BLE printer: which GATT service/characteristic carries the
/// print stream, how to chunk writes, the static capabilities, and which command language to encode with.
/// </summary>
public sealed record BlePrinterConfig
{
    /// <summary>GATT service UUID that exposes the print characteristic.</summary>
    public required string ServiceUuid { get; init; }

    /// <summary>GATT characteristic UUID the print stream is written to.</summary>
    public required string WriteCharacteristicUuid { get; init; }

    /// <summary>Optional characteristic that reports printer status / paper-out (notify). Not required for printing.</summary>
    public string? StatusCharacteristicUuid { get; init; }

    /// <summary>
    /// When true (default), writes use "write without response" - the most compatible mode for cheap
    /// thermal printers. Set false only if your printer requires acknowledged writes.
    /// </summary>
    public bool WriteWithoutResponse { get; init; } = true;

    /// <summary>
    /// Bytes per BLE write. When null the connection derives it from the negotiated MTU (MTU - 3),
    /// capped to a safe value. Cheap printers often have tiny buffers, so an explicit small value (e.g. 20-180) helps.
    /// </summary>
    public int? ChunkSize { get; init; }

    /// <summary>Delay inserted between chunked writes to let the printer's buffer drain. Default 20ms.</summary>
    public TimeSpan InterChunkDelay { get; init; } = TimeSpan.FromMilliseconds(20);

    /// <summary>Static capabilities for this printer (chars-per-line, paper width, etc).</summary>
    public required PrinterCapabilities Capabilities { get; init; }

    /// <summary>Factory for the command-language encoder. Defaults to <see cref="EscPosProtocol"/>.</summary>
    public Func<IPrinterProtocol> ProtocolFactory { get; init; } = static () => new EscPosProtocol();
}
