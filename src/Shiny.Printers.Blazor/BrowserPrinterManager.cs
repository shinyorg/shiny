using Microsoft.JSInterop;

namespace Shiny.Printers.Blazor;


/// <summary>
/// Connects to thermal printers from Blazor (WebAssembly or Server) over Web Bluetooth, Web Serial or
/// WebUSB, and hands back a ready-to-use <see cref="BrowserPrinter"/>.
/// </summary>
/// <remarks>
/// <para>
/// There is no scan seam here. Every browser device API opens a chooser that only the user can dismiss,
/// and it must be called from a user gesture (a click handler) - so <c>RequestBluetooth</c> /
/// <c>RequestSerial</c> / <c>RequestUsb</c> replace <c>IPrinterScanner</c> + <c>Connect</c> in one step.
/// Calling one outside a gesture makes the browser reject the request.
/// </para>
/// <para>
/// Register this scoped: on Blazor Server it holds a per-circuit JS module reference.
/// </para>
/// </remarks>
public sealed class BrowserPrinterManager(IJSRuntime jsRuntime) : IAsyncDisposable
{
    const string ModulePath = "./_content/Shiny.Printers.Blazor/shiny-printers.js";
    IJSObjectReference? module;


    /// <summary>
    /// Reports which transports this browser exposes. All three are Chromium-only and require a secure
    /// context, so check this before showing connect buttons rather than catching failures.
    /// </summary>
    public async Task<BrowserPrintingSupport> GetSupport(CancellationToken cancellationToken = default)
    {
        var js = await this.GetModule(cancellationToken).ConfigureAwait(false);
        return await js.InvokeAsync<BrowserPrintingSupport>("getSupport", cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// Opens the Web Bluetooth chooser filtered on the configured profiles, connects to the chosen
    /// printer and probes for its ESC/POS write characteristic.
    /// </summary>
    /// <exception cref="OperationCanceledException">The user dismissed the chooser.</exception>
    public Task<BrowserPrinter> RequestBluetooth(WebBluetoothPrinterConfig? config = null, CancellationToken cancellationToken = default)
    {
        var cfg = config ?? new WebBluetoothPrinterConfig();
        return this.Request(BrowserPrinterTransport.WebBluetooth, "requestBluetooth", cfg.ToJs(), cfg, cancellationToken);
    }


    /// <summary>Opens the Web Serial port chooser and opens the chosen port at the configured line settings.</summary>
    /// <exception cref="OperationCanceledException">The user dismissed the chooser.</exception>
    public Task<BrowserPrinter> RequestSerial(WebSerialPrinterConfig? config = null, CancellationToken cancellationToken = default)
    {
        var cfg = config ?? new WebSerialPrinterConfig();
        return this.Request(BrowserPrinterTransport.WebSerial, "requestSerial", cfg.ToJs(), cfg, cancellationToken);
    }


    /// <summary>
    /// Opens the WebUSB device chooser, claims the printer interface and locates its bulk OUT endpoint.
    /// Fails if the OS printer driver already owns the device - see <see cref="WebUsbPrinterConfig"/>.
    /// </summary>
    /// <exception cref="OperationCanceledException">The user dismissed the chooser.</exception>
    public Task<BrowserPrinter> RequestUsb(WebUsbPrinterConfig? config = null, CancellationToken cancellationToken = default)
    {
        var cfg = config ?? new WebUsbPrinterConfig();
        return this.Request(BrowserPrinterTransport.WebUsb, "requestUsb", cfg.ToJs(), cfg, cancellationToken);
    }


    async Task<BrowserPrinter> Request(
        BrowserPrinterTransport transport,
        string method,
        object jsConfig,
        BrowserPrinterConfig config,
        CancellationToken cancellationToken
    )
    {
        var js = await this.GetModule(cancellationToken).ConfigureAwait(false);
        var connection = new BrowserPrinterConnection(transport);

        try
        {
            await connection.Open(js, method, jsConfig, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        return new BrowserPrinter(connection, config.ProtocolFactory(), config.Capabilities);
    }


    async Task<IJSObjectReference> GetModule(CancellationToken cancellationToken)
        => this.module ??= await jsRuntime
            .InvokeAsync<IJSObjectReference>("import", cancellationToken, ModulePath)
            .ConfigureAwait(false);


    public async ValueTask DisposeAsync()
    {
        if (this.module is null)
            return;

        try
        {
            await this.module.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            // Circuit already gone - nothing to release.
        }
        finally
        {
            this.module = null;
        }
    }
}
