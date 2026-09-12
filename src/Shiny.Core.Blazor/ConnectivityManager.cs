using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Shiny.Net;

namespace Shiny.Infrastructure;


/// <summary>
/// The Blazor WebAssembly <see cref="IConnectivity"/>, backed by <c>navigator.onLine</c> and the
/// Network Information API.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Access"/> and <see cref="ConnectionTypes"/> are synchronous by contract, but the JS
/// module behind them has to be imported first. Rather than block, the load is kicked off the first
/// time anything touches this class - a property read or a <see cref="Changed"/> subscription - and
/// the properties report <see cref="NetworkAccess.Unknown"/> / <see cref="ConnectionTypes.Unknown"/>
/// until it completes. Call <see cref="StartAsync"/> (or <c>UseShinyCore()</c> on the built host) to
/// await that explicitly.
/// </para>
/// <para>
/// <c>navigator.onLine</c> is available everywhere, so <see cref="Access"/> is reliable. The
/// connection type comes from the Network Information API, which only Chromium-based browsers
/// implement - elsewhere it stays <see cref="ConnectionTypes.Unknown"/>.
/// </para>
/// </remarks>
public class ConnectivityManager(IJSRuntime jsRuntime, ILogger<ConnectivityManager> logger) : IConnectivity, IAsyncDisposable
{
    IJSObjectReference? module;
    IJSInProcessObjectReference? inProcess;
    DotNetObjectReference<ConnectivityManager>? objRef;
    Task? startTask;


    public event EventHandler? Changed;


    [JSInvokable]
    public void OnChange() => this.Changed?.Invoke(this, EventArgs.Empty);


    public ConnectionTypes ConnectionTypes
    {
        get
        {
            this.EnsureStarted();
            if (this.inProcess == null)
                return ConnectionTypes.Unknown;

            var type = this.inProcess.Invoke<string>("getConnType");
            return type switch
            {
                "bluetooth" => ConnectionTypes.Bluetooth,
                "ethernet" => ConnectionTypes.Wired,
                "cellular" => ConnectionTypes.Cellular,
                "wifi" => ConnectionTypes.Wifi,
                "wimax" => ConnectionTypes.Wifi,
                "none" => ConnectionTypes.None,
                _ => ConnectionTypes.Unknown
            };
        }
    }


    public NetworkAccess Access
    {
        get
        {
            this.EnsureStarted();
            if (this.inProcess == null)
                return NetworkAccess.Unknown;

            return this.inProcess.Invoke<bool>("isConnected")
                ? NetworkAccess.Internet
                : NetworkAccess.None;
        }
    }


    /// <summary>
    /// Imports the JS module, reads the browser connection object, and starts listening for
    /// changes. Safe to call more than once - subsequent calls await the same operation.
    /// </summary>
    public Task StartAsync() => this.startTask ??= this.Start();


    async Task Start()
    {
        var mod = await jsRuntime
            .InvokeAsync<IJSObjectReference>("import", "./_content/Shiny.Core.Blazor/connectivity.js")
            .ConfigureAwait(false);

        // resolves navigator.connection - without it, getConnType() never reports anything
        // but "unknown"
        await mod.InvokeVoidAsync("init").ConfigureAwait(false);

        this.objRef = DotNetObjectReference.Create(this);
        await mod.InvokeVoidAsync("startListener", this.objRef).ConfigureAwait(false);

        // published last: the synchronous properties above read these, so they must only
        // see a module that is fully initialized
        this.inProcess = mod as IJSInProcessObjectReference;
        this.module = mod;
    }


    void EnsureStarted()
    {
        if (this.startTask != null)
            return;

        _ = this.StartAsync().ContinueWith(
            t => logger.LogWarning(t.Exception, "Failed to start connectivity monitoring"),
            TaskContinuationOptions.OnlyOnFaulted
        );
    }


    public async ValueTask DisposeAsync()
    {
        if (this.module != null)
        {
            try
            {
                await this.module.InvokeVoidAsync("stopListener").ConfigureAwait(false);
            }
            catch { }
        }
        this.objRef?.Dispose();
        this.objRef = null;

        if (this.module != null)
        {
            await this.module.DisposeAsync().ConfigureAwait(false);
            this.module = null;
        }
        this.inProcess = null;
        this.startTask = null;
    }
}
