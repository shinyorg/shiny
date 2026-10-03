namespace Shiny.Printers.Blazor;


/// <summary>
/// Which browser transports are actually available to the current page. Every one of these APIs is
/// Chromium-only (no Safari, no Firefox) and all three require a secure context, so feature-detect with
/// <see cref="BrowserPrinterManager.GetSupport"/> before offering a connect button.
/// </summary>
/// <param name="Bluetooth"><c>navigator.bluetooth</c> is present (Chrome / Edge / Opera, desktop + Android).</param>
/// <param name="Serial"><c>navigator.serial</c> is present (Chrome / Edge desktop only).</param>
/// <param name="Usb"><c>navigator.usb</c> is present (Chrome / Edge desktop + Android).</param>
/// <param name="SecureContext">The page is running in a secure context (HTTPS or localhost).</param>
public sealed record BrowserPrintingSupport(bool Bluetooth, bool Serial, bool Usb, bool SecureContext)
{
    /// <summary>True when at least one transport can be used.</summary>
    public bool Any => this.Bluetooth || this.Serial || this.Usb;

    /// <summary>The transports available, ready to bind to a picker.</summary>
    public IReadOnlyList<BrowserPrinterTransport> Available
    {
        get
        {
            var list = new List<BrowserPrinterTransport>(3);
            if (this.Bluetooth) list.Add(BrowserPrinterTransport.WebBluetooth);
            if (this.Serial) list.Add(BrowserPrinterTransport.WebSerial);
            if (this.Usb) list.Add(BrowserPrinterTransport.WebUsb);
            return list;
        }
    }
}
