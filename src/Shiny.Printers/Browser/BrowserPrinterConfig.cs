using Shiny.Printers.Protocols.EscPos;

namespace Shiny.Printers.Blazor;


/// <summary>
/// Shared settings for every browser transport: the static capabilities to attach to the connected
/// printer, which command language to encode with, and how hard to push bytes at the device.
/// </summary>
public abstract record BrowserPrinterConfig
{
    /// <summary>Static capabilities for this printer (chars-per-line, paper width, etc). Defaults to 58mm.</summary>
    public PrinterCapabilities Capabilities { get; init; } = PrinterCapabilities.Paper58mm;

    /// <summary>Factory for the command-language encoder. Defaults to <see cref="EscPosProtocol"/>.</summary>
    public Func<IPrinterProtocol> ProtocolFactory { get; init; } = static () => new EscPosProtocol();

    /// <summary>Delay inserted between chunked writes to let the printer's buffer drain. Default 20ms.</summary>
    public TimeSpan InterChunkDelay { get; init; } = TimeSpan.FromMilliseconds(20);

    /// <summary>Bytes per write to the underlying transport.</summary>
    public abstract int ChunkSize { get; init; }

    internal int InterChunkDelayMs => (int)Math.Max(0, this.InterChunkDelay.TotalMilliseconds);
}


/// <summary>
/// Web Bluetooth settings. The browser's own device chooser replaces the scan seam
/// (<c>IPrinterScanner</c>) used by the native BLE package - there is no background scanning on the web.
/// </summary>
public sealed record WebBluetoothPrinterConfig : BrowserPrinterConfig
{
    /// <summary>
    /// The GATT service/characteristic combinations to filter the chooser on and then probe, in order.
    /// Defaults to <see cref="KnownWebPrinterProfiles.All"/>.
    /// </summary>
    public IReadOnlyList<WebBluetoothProfile> Profiles { get; init; } = KnownWebPrinterProfiles.All;

    /// <summary>
    /// Show every nearby device in the chooser instead of filtering on <see cref="Profiles"/>. Useful
    /// for printers that don't advertise their print service; the profiles are still probed after connect.
    /// </summary>
    public bool AcceptAllDevices { get; init; }

    /// <summary>
    /// When true (default), writes use "write without response" - the most compatible mode for cheap
    /// thermal printers. Falls back automatically if the characteristic doesn't offer it.
    /// </summary>
    public bool WriteWithoutResponse { get; init; } = true;

    /// <summary>
    /// Bytes per GATT write. Web Bluetooth never exposes the negotiated MTU, so unlike the native BLE
    /// package this cannot be derived - 180 is a safe default for cheap printers.
    /// </summary>
    public override int ChunkSize { get; init; } = 180;
}


/// <summary>
/// Web Serial settings for USB-serial / RS-232 receipt printers - the classic POS wiring. Most cheap
/// thermal units run 9600 8N1; check the self-test printout for the actual rate.
/// </summary>
public sealed record WebSerialPrinterConfig : BrowserPrinterConfig
{
    /// <summary>USB vendor ids to filter the port chooser on. Empty (default) lists every serial port.</summary>
    public IReadOnlyList<int> UsbVendorIds { get; init; } = [];

    /// <summary>Baud rate. Default 9600; 19200, 38400 and 115200 are also common.</summary>
    public int BaudRate { get; init; } = 9600;

    /// <summary>Data bits (7 or 8). Default 8.</summary>
    public int DataBits { get; init; } = 8;

    /// <summary>Stop bits (1 or 2). Default 1.</summary>
    public int StopBits { get; init; } = 1;

    /// <summary>Parity. Default <see cref="WebSerialParity.None"/>.</summary>
    public WebSerialParity Parity { get; init; } = WebSerialParity.None;

    /// <summary>
    /// Flow control. Default <see cref="WebSerialFlowControl.None"/>; set
    /// <see cref="WebSerialFlowControl.Hardware"/> (RTS/CTS) if long receipts come out truncated.
    /// </summary>
    public WebSerialFlowControl FlowControl { get; init; } = WebSerialFlowControl.None;

    /// <summary>Read/write buffer size in bytes. 0 (default) leaves the browser default in place.</summary>
    public int BufferSize { get; init; }

    /// <summary>Bytes per write. Serial has no packet limit, but small chunks keep slow printers fed.</summary>
    public override int ChunkSize { get; init; } = 1024;

    internal string ParityValue => this.Parity switch
    {
        WebSerialParity.Even => "even",
        WebSerialParity.Odd => "odd",
        _ => "none"
    };

    internal string FlowControlValue => this.FlowControl == WebSerialFlowControl.Hardware ? "hardware" : "none";
}


/// <summary>A device filter for the WebUSB chooser. At least one field should be set.</summary>
/// <param name="VendorId">USB vendor id.</param>
/// <param name="ProductId">USB product id.</param>
/// <param name="ClassCode">USB interface class; 7 is the printer class.</param>
public sealed record WebUsbFilter(int? VendorId = null, int? ProductId = null, int? ClassCode = null);


/// <summary>
/// WebUSB settings. The connection claims a bulk OUT endpoint directly, which means it competes with
/// the OS printer driver - on Windows and macOS a printer installed in the system print queue usually
/// cannot be claimed. Prefer <see cref="WebSerialPrinterConfig"/> where the device offers a serial port.
/// </summary>
public sealed record WebUsbPrinterConfig : BrowserPrinterConfig
{
    /// <summary>
    /// Filters for the device chooser. Defaults to the USB printer class (7), which covers most
    /// ESC/POS units; add a vendor id for devices that expose a vendor-specific interface instead.
    /// </summary>
    public IReadOnlyList<WebUsbFilter> Filters { get; init; } = [new(ClassCode: 7)];

    /// <summary>Bytes per bulk transfer. Default 4096 - USB handles its own packetisation.</summary>
    public override int ChunkSize { get; init; } = 4096;
}
