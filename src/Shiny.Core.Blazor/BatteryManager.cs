using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Shiny.Power;

namespace Shiny.Infrastructure;


/// <summary>
/// The Blazor WebAssembly <see cref="IBattery"/>, backed by the browser Battery Status API.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IBattery.Status"/> and <see cref="IBattery.Level"/> are synchronous by contract, but
/// the JS module behind them has to be imported and the <c>navigator.getBattery()</c> promise
/// awaited first. Rather than block, the load is kicked off the first time anything touches this
/// class - a property read or a <see cref="Changed"/> subscription - and the properties report
/// <see cref="BatteryState.Unknown"/> until it completes. Call <see cref="StartAsync"/> (or
/// <c>UseShinyCore()</c> on the built host) to await that explicitly.
/// </para>
/// <para>
/// Only Chromium-based browsers implement the API. Where it is missing, <see cref="Status"/> stays
/// <see cref="BatteryState.Unknown"/> and <see cref="Level"/> reports 1.0 rather than pretending the
/// device is discharging.
/// </para>
/// </remarks>
public class BatteryManager(IJSRuntime jsRuntime, ILogger<BatteryManager> logger) : IBattery, IAsyncDisposable
{
    IJSObjectReference? module;
    IJSInProcessObjectReference? inProcess;
    DotNetObjectReference<BatteryManager>? objRef;
    Task? startTask;


    public event EventHandler? Changed;


    [JSInvokable]
    public void OnChange() => this.Changed?.Invoke(this, EventArgs.Empty);


    public BatteryState Status
    {
        get
        {
            this.EnsureStarted();
            if (this.inProcess == null || !this.inProcess.Invoke<bool>("isSupported"))
                return BatteryState.Unknown;

            return this.inProcess.Invoke<bool>("isCharging")
                ? BatteryState.Charging
                : BatteryState.Discharging;
        }
    }


    public double Level
    {
        get
        {
            this.EnsureStarted();
            if (this.inProcess == null)
                return 1.0;

            return this.inProcess.Invoke<double>("getLevel");
        }
    }


    /// <summary>
    /// Imports the JS module, initializes the browser battery object, and starts listening for
    /// changes. Safe to call more than once - subsequent calls await the same operation.
    /// </summary>
    public Task StartAsync() => this.startTask ??= this.Start();


    async Task Start()
    {
        var mod = await jsRuntime
            .InvokeAsync<IJSObjectReference>("import", "./_content/Shiny.Core.Blazor/battery.js")
            .ConfigureAwait(false);

        // the module resolves navigator.getBattery() here - without it, every read below
        // reports a device that is neither charging nor discharging
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
            t => logger.LogWarning(t.Exception, "Failed to start battery monitoring"),
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
