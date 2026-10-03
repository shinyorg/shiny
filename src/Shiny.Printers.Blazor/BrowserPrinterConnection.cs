using System.Reactive.Subjects;
using Microsoft.JSInterop;

namespace Shiny.Printers.Blazor;


/// <summary>
/// An <see cref="IPrinterConnection"/> backed by a browser transport (Web Bluetooth, Web Serial or
/// WebUSB). The JS module owns the transport specifics and exposes one uniform write/disconnect shape,
/// so this class is identical for all three.
/// </summary>
/// <remarks>
/// Chunking happens in JavaScript rather than here: one interop call carries the whole encoded document
/// and the module loops, which avoids a round-trip per chunk. The trade-off is that
/// <paramref name="cancellationToken"/> on <see cref="SendAsync"/> can only cancel the call as a whole -
/// once the bytes are handed over, the chunk loop runs to completion.
/// </remarks>
public sealed class BrowserPrinterConnection : IPrinterConnection, IAsyncDisposable
{
    readonly BehaviorSubject<PrinterConnectionState> status = new(PrinterConnectionState.Disconnected);
    DotNetObjectReference<BrowserPrinterConnection>? selfRef;
    IJSObjectReference? handle;

    internal BrowserPrinterConnection(BrowserPrinterTransport transport) => this.Transport = transport;


    /// <summary>Which browser API this connection is using.</summary>
    public BrowserPrinterTransport Transport { get; }

    /// <summary>The device name the browser reported (never null; falls back to a generic label).</summary>
    public string Name { get; private set; } = "";

    /// <summary>
    /// Opaque device id: the Web Bluetooth device id, or <c>vendor:product</c> for Serial and USB.
    /// Empty when the browser does not expose one.
    /// </summary>
    public string Id { get; private set; } = "";

    /// <summary>Transport-specific detail for diagnostics - matched BLE profile, baud rate, or USB endpoint.</summary>
    public string? Detail { get; private set; }

    public bool IsConnected => this.status.Value == PrinterConnectionState.Connected;

    public IObservable<PrinterConnectionState> WhenStatusChanged() => this.status;


    internal async Task Open(IJSObjectReference module, string method, object config, CancellationToken cancellationToken)
    {
        this.selfRef = DotNetObjectReference.Create(this);
        this.status.OnNext(PrinterConnectionState.Connecting);

        try
        {
            // The module returns null (rather than throwing) when the user dismisses the device chooser.
            var opened = await module
                .InvokeAsync<IJSObjectReference?>(method, cancellationToken, config, this.selfRef)
                .ConfigureAwait(false);

            if (opened is null)
                throw new OperationCanceledException("The user dismissed the browser device chooser.");

            var description = await opened
                .InvokeAsync<BrowserPrinterDescription>("describe", cancellationToken)
                .ConfigureAwait(false);

            this.handle = opened;
            this.Name = description.Name;
            this.Id = description.Id;
            this.Detail = description.Detail;
            this.status.OnNext(PrinterConnectionState.Connected);
        }
        catch
        {
            this.status.OnNext(PrinterConnectionState.Disconnected);
            this.selfRef.Dispose();
            this.selfRef = null;
            throw;
        }
    }


    public async ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var js = this.handle;
        if (js is null || !this.IsConnected)
            throw new InvalidOperationException("The printer is not connected.");

        if (data.IsEmpty)
            return;

        // Byte arrays cross the interop boundary as a Uint8Array - no base64 round-trip needed.
        await js.InvokeVoidAsync("write", cancellationToken, data.ToArray()).ConfigureAwait(false);
    }


    /// <summary>Invoked from JavaScript when the browser reports the device has gone away.</summary>
    [JSInvokable]
    public void OnDisconnected()
    {
        if (this.status.Value != PrinterConnectionState.Disconnected)
            this.status.OnNext(PrinterConnectionState.Disconnected);
    }


    /// <summary>Closes the transport and releases the browser's hold on the device.</summary>
    public async ValueTask Disconnect()
    {
        var js = this.handle;
        this.handle = null;
        if (js is null)
            return;

        try
        {
            await js.InvokeVoidAsync("disconnect").ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            // Blazor Server circuit already gone - the browser tore the device down with the page.
        }
        finally
        {
            try
            {
                await js.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
            }

            this.OnDisconnected();
        }
    }


    public async ValueTask DisposeAsync()
    {
        await this.Disconnect().ConfigureAwait(false);
        this.selfRef?.Dispose();
        this.selfRef = null;
        this.status.Dispose();
    }
}
