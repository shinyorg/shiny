---
name: shiny-printing
description: Generate code using Shiny.Printers and Shiny.Printing for .NET - thermal / receipt printing (ESC/POS, TSPL) with a fluent PrintDocument over Bluetooth LE, WiFi (raw TCP 9100 + mDNS discovery) and browser Web Bluetooth / Web Serial / WebUSB, plus OS-native printing of PDF, image, HTML and files through AirPrint, Android PrintManager, Windows GDI+/shell, CUPS and window.print, and rendering a receipt to PDF with SkiaSharp
auto_invoke: true
triggers:
  - print
  - printing
  - printer
  - print a receipt
  - receipt printer
  - thermal printer
  - POS printer
  - ESC/POS
  - escpos
  - TSPL
  - label printer
  - bluetooth printer
  - BLE printer
  - wifi printer
  - network printer
  - port 9100
  - JetDirect
  - print barcode
  - print QR code
  - cash drawer
  - AirPrint
  - print dialog
  - print a PDF
  - print HTML
  - print an image
  - silent print
  - list printers
  - CUPS
  - lp command
  - window.print
  - Web Serial printer
  - WebUSB printer
  - PrintDocument
  - PrinterCapabilities
  - Paper58mm
  - Paper80mm
  - IPrinter
  - IPrinterConnection
  - IPrinterProtocol
  - EscPosProtocol
  - TsplProtocol
  - PrinterImage
  - ImageDithering
  - IPrinterScanner
  - BlePrinterManager
  - BlePrinterConfig
  - KnownPrinterProfiles
  - DiscoveredPrinter
  - INetworkPrinterScanner
  - NetworkPrinterManager
  - NetworkPrinterConfig
  - DiscoveredNetworkPrinter
  - TcpPrinterConnection
  - BrowserPrinterManager
  - BrowserPrinter
  - WebBluetoothPrinterConfig
  - WebSerialPrinterConfig
  - WebUsbPrinterConfig
  - IPrintService
  - PrintJob
  - PrintOptions
  - PrintResult
  - PrintingCapabilities
  - IPrintDocumentRenderer
  - PrintRenderOptions
  - AddBluetoothLePrinting
  - AddNetworkPrinting
  - AddBrowserPrinting
  - AddNativePrinting
  - AddBlazorPrinting
  - AddPrintDocumentRendering
  - Shiny.Printers
  - Shiny.Printing
---

# Shiny Printing

Two **separate** printing models. Pick by the printer, not by the platform:

| You have | Use | What it does |
|---|---|---|
| A 58/80mm receipt, label or kitchen printer | **`Shiny.Printers`** + a transport package | You build a `PrintDocument`; an encoder turns it into ESC/POS (or TSPL) bytes; a transport streams them. The app owns layout. |
| Any printer the OS knows about (AirPrint, laser, inkjet, CUPS queue) | **`Shiny.Printing`** | You hand a PDF / image / HTML / file to the OS print pipeline (dialog, spooler, driver). |

They meet in one place: `Shiny.Printing.Rendering` renders a thermal `PrintDocument` to a PDF, so the same receipt
can go to either.

## Packages

| Package | Platforms | Purpose |
|---|---|---|
| `Shiny.Printers` | any .NET | Document model, `PrinterCapabilities`, `EscPosProtocol`, `TsplProtocol`, `PrinterImage`, plus the Blazor (WASM / Server) Web Bluetooth, Web Serial and WebUSB transports (`AddBrowserPrinting()`, namespace `Shiny.Printers.Blazor`). AOT/trim clean. |
| `Shiny.Printers.BluetoothLE` | iOS, Mac Catalyst, macOS, Android, Windows, Linux | Scan + connect BLE printers via `Shiny.BluetoothLE`, MTU-chunked writes. |
| `Shiny.Printers.Network` | everywhere incl. Blazor **Server** | Raw TCP (port 9100) + mDNS discovery via `Shiny.Net.Discovery`. |
| `Shiny.Printing` | iOS, Mac Catalyst, Android, Windows, Linux, macOS, Blazor | `IPrintService` - AirPrint / `PrintManager` / GDI+ + shell / CUPS, and `window.print()` on Blazor (`AddNativePrinting()` picks it on WebAssembly; `AddBlazorPrinting()` for Blazor Server). |
| `Shiny.Printing.Rendering` | any .NET | `IPrintDocumentRenderer` - `PrintDocument` -> PDF with SkiaSharp. |

## Registration

Every `Add*` extension lives in `namespace Shiny`.

```csharp
// MauiProgram.cs (iOS, Mac Catalyst, macOS, Android, Windows)
builder.Services.AddBluetoothLePrinting();     // also registers AddBluetoothLE() on these platforms
builder.Services.AddNetworkPrinting();         // also registers AddMdns() unless already registered
builder.Services.AddNativePrinting();          // the platform IPrintService
builder.Services.AddPrintDocumentRendering();  // optional: PrintDocument -> PDF
```

```csharp
// Linux (Shiny.BluetoothLE.Linux) - BLE must be registered FIRST or AddBluetoothLePrinting throws
services.AddBluetoothLE();
services.AddBluetoothLePrinting();
services.AddNetworkPrinting();
services.AddNativePrinting();                  // CUPS
```

```csharp
// Blazor
builder.Services.AddBrowserPrinting();   // BrowserPrinterManager (scoped)
builder.Services.AddBlazorPrinting();    // IPrintService over window.print()
```

Calling `AddBluetoothLE<TDelegate>()` yourself as well as `AddBluetoothLePrinting()` is fine - BLE registration is idempotent.

## Thermal printing

### Build a document

```csharp
using Shiny.Printers;
using Shiny.Printers.Document;

PrintDocument BuildReceipt(PrinterCapabilities caps)
{
    var doc = new PrintDocument()
        .AlignCenter().Bold().Size(2, 2).Line("SHINY MART").ResetStyle()
        .AlignLeft().Line(new string('-', caps.CharactersPerLine))   // size layout to the printer
        .Line(Columns("Coffee", "$3.50", caps.CharactersPerLine))
        .Bold().Line(Columns("TOTAL", "$3.50", caps.CharactersPerLine)).Bold(false)
        .AlignCenter()
        .Barcode(BarcodeFormat.Code128, "ORDER-100425", height: 70)
        .QrCode("https://example.com/r/100425", moduleSize: 6)
        .Feed(2);

    if (caps.SupportsCut)   // feature-detect - cheap 58mm printers have no cutter
        doc.Cut();

    return doc;
}

static string Columns(string left, string right, int width)
{
    var space = width - left.Length - right.Length;
    return space < 1 ? $"{left} {right}" : left + new string(' ', space) + right;
}
```

Rules:
- **Lay out against `printer.Capabilities`**, never hard-coded widths. `CharactersPerLine` is the column count at 1x
  (32 on 58mm, 48 on 80mm); `DotsPerLine` is the max image width (384 / 576).
- `Size(w, h)` takes 1-8. `ResetStyle()` resets alignment, bold, underline and size.
- `WrapText(text, caps.CharactersPerLine)` word-wraps long text. `Raw(bytes)` is the escape hatch for
  printer-specific commands (cash drawer kick, code pages).
- Non-ASCII text: set `EscPosProtocol.Encoding` and the matching `CodePage` - the default is ASCII / PC437.

### Images

The core never decodes PNG/JPEG. Decode to RGBA yourself (SkiaSharp on MAUI, canvas in the browser) and convert:

```csharp
var image = PrinterImage.FromPixels(rgbaSpan, width, height, ImageDithering.FloydSteinberg);
if (image.Width <= printer.Capabilities.DotsPerLine)   // wider than the printhead does not print
    doc.AlignCenter().Image(image);
```

`ImageDithering.Threshold` for logos / line art, `FloydSteinberg` for photos. RGBA byte order is R, G, B, A;
transparency is composited onto white. With SkiaSharp decode as `SKColorType.Rgba8888` / `SKAlphaType.Unpremul`.

### Bluetooth LE

```csharp
public class ReceiptService(IPrinterScanner scanner, BlePrinterManager manager)
{
    public async Task PrintFirstPrinter(PrintDocument doc)
    {
        // Scan() requests BLE access, filters on KnownPrinterProfiles, and never completes on its own
        var found = await scanner.Scan().Take(1).Timeout(TimeSpan.FromSeconds(15)).ToTask();

        var printer = await manager.Connect(found);
        try
        {
            await printer.Print(doc);
        }
        finally
        {
            (printer as IDisposable)?.Dispose();   // a Printer - disposing disconnects the peripheral
        }
    }
}
```

- `DiscoveredPrinter` = `Peripheral`, `Name`, `Rssi`, matched `Config`. The same printer is emitted repeatedly -
  de-duplicate on `Uuid`.
- Unknown printer? Add a profile to `KnownPrinterProfiles.All` at startup, or connect with an explicit config:

```csharp
var printer = await manager.Connect(peripheralUuid, new BlePrinterConfig
{
    ServiceUuid = "0000ff00-0000-1000-8000-00805f9b34fb",
    WriteCharacteristicUuid = "0000ff02-0000-1000-8000-00805f9b34fb",
    Capabilities = PrinterCapabilities.Paper58mm,
    ChunkSize = 100,                                  // null = MTU - 3
    InterChunkDelay = TimeSpan.FromMilliseconds(30)   // let a small buffer drain
});
```

Garbled or truncated output on a cheap printer: lower `ChunkSize` and raise `InterChunkDelay`.

### WiFi / Ethernet

```csharp
// fixed install - connect by address
var printer = await networkManager.Connect("192.168.1.50", 9100, PrinterCapabilities.Paper80mm);

// or discover over mDNS (_pdl-datastream._tcp + _printer._tcp, de-duplicated by host:port)
var found = await networkScanner.Scan().Take(1).Timeout(TimeSpan.FromSeconds(10)).ToTask();
var printer = await networkManager.Connect(found);
```

- `DiscoveredNetworkPrinter.Capabilities` defaults to **80mm** - mDNS never advertises paper width. Use
  `found with { Capabilities = PrinterCapabilities.Paper58mm }` when you know better.
- `Scan(serviceType)` browses a vendor-specific DNS-SD type.
- Same `PrintDocument`, same disposal: `(printer as IDisposable)?.Dispose()` closes the socket.

### Blazor (browser transports)

```razor
@inject BrowserPrinterManager Printers
@implements IAsyncDisposable

<button @onclick="Connect" disabled="@(!support.Serial)">Connect printer</button>

@code {
    BrowserPrintingSupport support = new(false, false, false, false);
    BrowserPrinter? printer;

    protected override async Task OnAfterRenderAsync(bool first)
    {
        if (first)
        {
            support = await Printers.GetSupport();   // JS interop only after first render
            StateHasChanged();
        }
    }

    async Task Connect()   // MUST run from a click - browsers reject device choosers otherwise
    {
        try
        {
            printer = await Printers.RequestSerial(new() { BaudRate = 9600, Capabilities = PrinterCapabilities.Paper80mm });
            await printer.Print(new PrintDocument().Line("Hello").Feed(3));
        }
        catch (OperationCanceledException)
        {
            // user dismissed the chooser
        }
    }

    public ValueTask DisposeAsync() => printer?.DisposeAsync() ?? ValueTask.CompletedTask;
}
```

- **No scanner on the web.** `RequestBluetooth()` / `RequestSerial()` / `RequestUsb()` open the browser chooser and
  connect in one call. Never call them from `OnInitialized` / `OnAfterRender`.
- Chromium only (Chrome, Edge, Opera), secure context only (HTTPS or localhost), never iOS. Feature-detect with
  `GetSupport()`.
- Web Bluetooth cannot read the MTU, so `WebBluetoothPrinterConfig.ChunkSize` defaults to 180.
- WebUSB fights the OS printer driver - prefer Web Serial where the device has a port.
- Port-9100 printers are unreachable from WASM (no raw sockets). Under **Blazor Server** use
  `Shiny.Printers.Network` - the socket runs server-side.

### Custom transport or protocol

Implement `IPrinterConnection` (`IsConnected`, `WhenStatusChanged()` replaying the current state,
`SendAsync(ReadOnlyMemory<byte>)` honouring the transport's payload limit) or `IPrinterProtocol`
(`Encode(PrintDocument, PrinterCapabilities) => byte[]`), then compose: `new Printer(connection, protocol, caps)`.

## OS-native printing

```csharp
public class DocumentPrinter(IPrintService print, IPrintDocumentRenderer renderer)
{
    public async Task Print(byte[] pdf)
    {
        var result = await print.Print(PrintJob.Pdf(pdf, new() { JobName = "Invoice 1042" }));
        if (!result.IsSuccess)
            Console.WriteLine($"{result.Status}: {result.Error}");
    }

    // the thermal receipt, on an office printer
    public Task<PrintResult> PrintReceipt(PrintDocument receipt)
        => print.Print(PrintJob.Pdf(renderer.RenderToPdf(receipt, PrintRenderOptions.Letter)));

    // desktop / CUPS: no dialog, straight to a queue
    public async Task PrintSilently(byte[] pdf)
    {
        if (!print.Capabilities.HasFlag(PrintingCapabilities.Silent | PrintingCapabilities.EnumeratePrinters))
            return;

        var target = (await print.GetPrinters()).FirstOrDefault(x => x.IsDefault);
        await print.Print(PrintJob.Pdf(pdf, new() { PreferSilent = true, PrinterId = target?.Id }));
    }

    // HTML laid out exactly as printing would, saved as a PDF to share instead (iOS / Mac Catalyst / Android)
    public async Task<string> SavePdf(string html)
    {
        var pdf = await print.HtmlToPdf(html, PdfPageOptions.Letter with { Orientation = PrintOrientation.Landscape });
        var path = Path.Combine(FileSystem.CacheDirectory, "callsheet.pdf");
        await File.WriteAllBytesAsync(path, pdf);
        return path;
    }
}
```

- Jobs: `PrintJob.Pdf(bytes | stream)`, `PrintJob.Image(png/jpeg bytes)`, `PrintJob.Html(markup)`,
  `PrintJob.HtmlUrl(uri)`, `PrintJob.File(path)` (routed by extension).
- `Print` does **not** throw for platform limits - it returns `PrintResult` with `Status`
  (`Completed`, `Submitted`, `Cancelled`, `Failed`) and `Error`. `IsSuccess` covers `Completed` and `Submitted`.
- **Feature-detect with `IPrintService.Capabilities`** before offering a button:

| Platform | Backend | PDF | Image | HTML | HtmlToPdf | Silent | Enumerate |
|---|---|:-:|:-:|:-:|:-:|:-:|:-:|
| iOS / Mac Catalyst | AirPrint `UIPrintInteractionController` (+ `WKWebView` for HTML) | yes | yes | yes | yes | to a previously picked `UIPrinter` URL | no |
| Android | `PrintManager` (+ `WebView` for HTML) | yes | yes | yes | yes | no - always the system dialog | no |
| Windows | GDI+ (images) + shell `print`/`printto` (PDF) | yes | yes | no | no | yes | yes |
| Linux / macOS (non-Catalyst) | CUPS `lp` / `lpstat` | yes | yes | no | no | yes (always direct to queue) | yes |
| Blazor | `window.print()` | yes | yes | yes | no | no | no |

- HTML (`PrintJob.Html` and `PrintJob.HtmlUrl`) renders through a real browser engine on every platform that
  supports it, so flexbox and CSS `page-break-*` rules survive printing.
- HTML on Windows / CUPS is unsupported - render to PDF first.
- `HtmlToPdf(html, PdfPageOptions)` returns PDF bytes with no UI; it throws `PlatformNotSupportedException` where
  `PrintingCapabilities.HtmlToPdf` is not set. `PdfPageOptions` is A4 portrait with 36pt margins by default
  (`PdfPageOptions.Letter` preset; `Orientation = Landscape` turns the page). Android also honours a CSS
  `@page { margin }` in the document, which wins over `Margin` there; iOS uses `Margin` only.
- Windows PDFs print through the registered PDF handler's shell verb; a machine with no PDF app fails.
- `PrintOrientation`, `PrintDuplex`, `PrintColorMode`, `Copies` go in `PrintOptions`. On Android only
  `Orientation` is applied - it sets the dialog's starting paper (the locale's Letter / A4, turned); the user
  chooses the rest in the dialog.

## Platform setup

**iOS / Mac Catalyst** `Info.plist`:

```xml
<key>NSBluetoothAlwaysUsageDescription</key>
<string>Prints receipts to nearby Bluetooth printers</string>
<key>NSLocalNetworkUsageDescription</key>
<string>Finds and prints to receipt printers on your network</string>
<key>NSBonjourServices</key>
<array>
    <string>_pdl-datastream._tcp</string>
    <string>_printer._tcp</string>
</array>
```

Without the Bonjour entries iOS returns nothing from the browse, and without `NSLocalNetworkUsageDescription` it
blocks the socket to the printer too.

**macOS (AppKit, sandboxed)**: the same Info.plist keys, plus `com.apple.security.network.client`,
`com.apple.security.device.bluetooth` and `com.apple.security.print` (CUPS) entitlements.

**Android**: `BLUETOOTH_SCAN` / `BLUETOOTH_CONNECT` (API 31+), `INTERNET`, `ACCESS_NETWORK_STATE`. Native printing
needs nothing - it runs from the current foreground activity, so call it while the app is visible.

**Linux**: add `SkiaSharp.NativeAssets.Linux` to the **app** when using `Shiny.Printing.Rendering` - SkiaSharp's plain
`net10.0` build ships only macOS and Windows natives, so rendering throws `DllNotFoundException: libSkiaSharp`
without it. CUPS printing needs `lp`/`lpstat` (the `cups-client` package).

## Don't

- Don't hard-code 32 or 48 columns - read `CharactersPerLine`.
- Don't call `Cut()` without checking `SupportsCut`.
- Don't send an image wider than `DotsPerLine`.
- Don't wrap `IPrintService.Print` in try/catch to detect platform support - check `Capabilities`.
- Don't use `Shiny.Printing` for a thermal printer (no driver, garbage output) or `Shiny.Printers` for an office
  printer (it does not speak ESC/POS).
- Don't forget to dispose the printer a manager returns - the BLE link / socket stays open otherwise.
