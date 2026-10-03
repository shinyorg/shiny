using System.Linq;

namespace Shiny.Printers.Blazor;

// The public configs carry a ProtocolFactory delegate and a PrinterCapabilities graph, neither of which
// can (or should) cross the JS interop boundary. These are the flat, serialisable projections handed to
// shiny-printers.js - Blazor's default interop options camelCase the property names.

sealed record BluetoothProfileJs(string Name, string Service, string? Write);

sealed record BluetoothConfigJs(
    IReadOnlyList<BluetoothProfileJs> Profiles,
    bool AcceptAllDevices,
    bool WriteWithoutResponse,
    int ChunkSize,
    int InterChunkDelayMs
);

sealed record SerialConfigJs(
    IReadOnlyList<int> UsbVendorIds,
    int BaudRate,
    int DataBits,
    int StopBits,
    string Parity,
    string FlowControl,
    int BufferSize,
    int ChunkSize,
    int InterChunkDelayMs
);

sealed record UsbFilterJs(int? VendorId, int? ProductId, int? ClassCode);

sealed record UsbConfigJs(
    IReadOnlyList<UsbFilterJs> Filters,
    int ChunkSize,
    int InterChunkDelayMs
);

/// <summary>What the JS connection object reports about the device it opened.</summary>
sealed record BrowserPrinterDescription(string Transport, string Name, string Id, string? Detail);


static class JsConfigMapper
{
    public static BluetoothConfigJs ToJs(this WebBluetoothPrinterConfig config)
    {
        if (config.Profiles.Count == 0)
            throw new ArgumentException("At least one Web Bluetooth profile is required.", nameof(config));

        return new(
            config.Profiles.Select(p => new BluetoothProfileJs(p.Name, p.ServiceUuid, p.WriteCharacteristicUuid)).ToArray(),
            config.AcceptAllDevices,
            config.WriteWithoutResponse,
            config.ChunkSize,
            config.InterChunkDelayMs
        );
    }

    public static SerialConfigJs ToJs(this WebSerialPrinterConfig config) => new(
        config.UsbVendorIds,
        config.BaudRate,
        config.DataBits,
        config.StopBits,
        config.ParityValue,
        config.FlowControlValue,
        config.BufferSize,
        config.ChunkSize,
        config.InterChunkDelayMs
    );

    public static UsbConfigJs ToJs(this WebUsbPrinterConfig config) => new(
        config.Filters.Select(f => new UsbFilterJs(f.VendorId, f.ProductId, f.ClassCode)).ToArray(),
        config.ChunkSize,
        config.InterChunkDelayMs
    );
}
