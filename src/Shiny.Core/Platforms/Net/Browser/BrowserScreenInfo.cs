using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Shiny.Infrastructure;


/// <summary>
/// The Blazor WebAssembly <see cref="IScreenInfo"/>, backed by <c>window.screen</c>, <c>devicePixelRatio</c> and
/// the Screen Orientation API. Also reports the browser viewport, which has no place on the cross-platform contract.
/// </summary>
/// <remarks>
/// Like the browser connectivity and battery monitors, the JS module is imported the first time anything touches
/// this class, and <see cref="IScreenInfo.Metrics"/> is <see cref="ScreenMetrics.Empty"/> until it has loaded. Call
/// <see cref="StartAsync"/> (or <c>UseShiny()</c> on the built host) to await that explicitly.
/// </remarks>
public class BrowserScreenInfo(IJSRuntime jsRuntime, ILogger<BrowserScreenInfo> logger) : AbstractScreenInfo, IAsyncDisposable
{
    IJSObjectReference? module;
    IJSInProcessObjectReference? inProcess;
    DotNetObjectReference<BrowserScreenInfo>? objRef;
    Task? startTask;


    /// <summary>
    /// The browser viewport width in CSS pixels (<c>window.innerWidth</c>), or 0 until the module has loaded.
    /// </summary>
    public int ViewportWidth => this.Invoke("getViewportWidth");


    /// <summary>
    /// The browser viewport height in CSS pixels (<c>window.innerHeight</c>), or 0 until the module has loaded.
    /// </summary>
    public int ViewportHeight => this.Invoke("getViewportHeight");


    [JSInvokable]
    public void OnChange() => this.Refresh();


    /// <summary>
    /// Imports the JS module and starts listening for resizes and orientation changes. Safe to call more than once.
    /// </summary>
    public Task StartAsync() => this.startTask ??= this.Start();


    async Task Start()
    {
        var mod = await jsRuntime
            .InvokeAsync<IJSObjectReference>("import", "./_content/Shiny.Core/screen.js")
            .ConfigureAwait(false);

        this.objRef = DotNetObjectReference.Create(this);
        await mod.InvokeVoidAsync("startListener", this.objRef).ConfigureAwait(false);

        // published last: the synchronous reads only see a module that is ready
        this.inProcess = mod as IJSInProcessObjectReference;
        this.module = mod;

        // anything subscribed before the module loaded saw Empty - this brings it up to date
        this.Refresh();
    }


    protected override ScreenMetrics ReadMetrics()
    {
        this.EnsureStarted();
        if (this.inProcess == null)
            return ScreenMetrics.Empty;

        var screen = this.inProcess.InvokeNoArgs<JsScreen>("getScreen");
        var width = screen.Width * screen.Density;
        var height = screen.Height * screen.Density;
        var orientation = screen.Orientation switch
        {
            "portrait" => DeviceOrientation.Portrait,
            "landscape" => DeviceOrientation.Landscape,
            _ => ScreenMetrics.OrientationOf(width, height)
        };

        // browsers expose no refresh rate
        return new ScreenMetrics(width, height, screen.Density, orientation, 0);
    }


    // listening starts with the module and costs nothing, so there is nothing to start or stop here
    protected override IDisposable Watch(Action refresh)
    {
        this.EnsureStarted();
        return new ActionDisposable(() => { });
    }


    protected override async Task<bool> ApplyOrientation(DeviceOrientation orientation)
    {
        await this.StartAsync().ConfigureAwait(false);
        var value = orientation switch
        {
            DeviceOrientation.Portrait => "portrait",
            DeviceOrientation.Landscape => "landscape",
            _ => null
        };
        return await this.module!.InvokeAsync<bool>("setOrientation", value).ConfigureAwait(false);
    }


    int Invoke(string function)
    {
        this.EnsureStarted();
        return this.inProcess?.InvokeNoArgs<int>(function) ?? 0;
    }


    void EnsureStarted()
    {
        if (this.startTask != null)
            return;

        _ = this.StartAsync().ContinueWith(
            t => logger.LogWarning(t.Exception, "Failed to start screen monitoring"),
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
                await this.module.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
            }
            this.module = null;
            this.inProcess = null;
        }
        this.objRef?.Dispose();
        this.objRef = null;
    }


    record JsScreen(double Width, double Height, double Density, string? Orientation);
}
