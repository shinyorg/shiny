using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Shiny.Infrastructure;


/// <summary>
/// The Blazor WebAssembly <see cref="IScreenPosture"/>, backed by the Device Posture API
/// (<c>navigator.devicePosture</c>) and the Viewport Segments API (<c>window.viewport.segments</c>) - Chromium on
/// foldables. The fold is the gap between two viewport segments; a <c>folded</c> posture reads as half open. The hinge
/// angle and occlusions are not available in a browser.
/// </summary>
/// <remarks>
/// <para>
/// CSS does not need this: <c>@media (device-posture: folded)</c> and <c>env(viewport-segment-*)</c> work directly.
/// This service is for C# logic.
/// </para>
/// <para>
/// Like the browser screen info, the JS module is imported the first time anything touches this class, and
/// <see cref="IScreenPosture.IsSupported"/> is false until it has loaded. Call <see cref="StartAsync"/> (or
/// <c>UseShiny()</c> on the built host) to await that explicitly.
/// </para>
/// </remarks>
public class BrowserScreenPosture(IJSRuntime jsRuntime, ILogger<BrowserScreenPosture> logger) : AbstractScreenPosture, IAsyncDisposable
{
    IJSObjectReference? module;
    IJSInProcessObjectReference? inProcess;
    DotNetObjectReference<BrowserScreenPosture>? objRef;
    Task? startTask;
    bool supported;


    public override bool IsSupported
    {
        get
        {
            this.EnsureStarted();
            return this.supported;
        }
    }


    [JSInvokable]
    public void OnChange() => this.Refresh();


    /// <summary>
    /// Imports the JS module and starts listening for posture and viewport changes. Safe to call more than once.
    /// </summary>
    public Task StartAsync() => this.startTask ??= this.Start();


    async Task Start()
    {
        var mod = await jsRuntime
            .InvokeAsync<IJSObjectReference>("import", "./_content/Shiny.Core/posture.js")
            .ConfigureAwait(false);

        this.objRef = DotNetObjectReference.Create(this);
        await mod.InvokeVoidAsync("startListener", this.objRef).ConfigureAwait(false);

        // published last: the synchronous reads only see a module that is ready
        this.inProcess = mod as IJSInProcessObjectReference;
        this.module = mod;
        this.supported = this.inProcess?.InvokeNoArgs<JsPosture>("getPosture").Supported ?? false;

        this.Refresh();
    }


    protected override ScreenPosture ReadPosture()
    {
        this.EnsureStarted();
        if (this.inProcess == null)
            return ScreenPosture.None;

        var posture = this.inProcess.InvokeNoArgs<JsPosture>("getPosture");
        return ToPosture(posture.Type, posture.Segments);
    }


    internal static ScreenPosture ToPosture(string? type, double[]? segments)
    {
        var folded = type == "folded";
        ScreenRect? fold = null;
        var orientation = FoldOrientation.None;

        if (segments is { Length: >= 8 })
        {
            var first = new ScreenRect(segments[0], segments[1], segments[2], segments[3]);
            var second = new ScreenRect(segments[4], segments[5], segments[6], segments[7]);

            // side by side is a vertical fold; stacked is a horizontal one. The fold is the gap between them, which is
            // zero wide where the display has no physical hinge.
            if (second.Left >= first.Right)
            {
                orientation = FoldOrientation.Vertical;
                fold = new ScreenRect(first.Right, first.Top, second.Left - first.Right, first.Height);
            }
            else if (second.Top >= first.Bottom)
            {
                orientation = FoldOrientation.Horizontal;
                fold = new ScreenRect(first.Left, first.Bottom, first.Width, second.Top - first.Bottom);
            }
        }

        if (!folded && fold == null)
            return ScreenPosture.None;

        return new ScreenPosture(
            folded ? FoldState.HalfOpen : FoldState.Flat,
            orientation,
            fold,
            fold != null,
            folded ? HingeStatus.PartiallyOpen : HingeStatus.FullyOpen,
            null,
            []
        );
    }


    // listening starts with the module and costs nothing, so there is nothing to start or stop here
    protected override IDisposable Watch(Action refresh)
    {
        this.EnsureStarted();
        return new ActionDisposable(() => { });
    }


    void EnsureStarted()
    {
        if (this.startTask != null)
            return;

        _ = this.StartAsync().ContinueWith(
            t => logger.LogWarning(t.Exception, "Failed to start screen posture monitoring"),
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


    record JsPosture(bool Supported, string? Type, double[]? Segments);
}
