using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Shiny.Gamepad.Infrastructure;

namespace Shiny.Gamepad;


/// <summary>What the browser supports, from a one-off feature check.</summary>
/// <param name="Supported">Whether <c>navigator.getGamepads</c> exists.</param>
/// <param name="SecureContext">Whether the page is a secure context, which some browsers require.</param>
public record BlazorGamepadProbe(bool Supported, bool SecureContext);


/// <summary>
/// Watches for controllers through the W3C Gamepad API.
/// </summary>
/// <remarks>
/// <para>The reading loop lives in JavaScript rather than in .NET, and that is the whole design.
/// The Gamepad API has no input event - <c>navigator.getGamepads()</c> returns a snapshot and the
/// page is expected to read it every frame - so driving it from .NET would mean an interop call per
/// frame per controller whether or not anything moved. Instead the browser side runs on
/// <c>requestAnimationFrame</c>, compares each snapshot with the last, and calls back only when
/// something changed. A player holding a controller still costs nothing.</para>
/// <para><b>Controllers stay invisible until the player presses something.</b> That is the
/// specification's anti-fingerprinting rule, not a bug and not something a permission prompt can
/// lift: a page that has never seen a gesture on a controller is told there is no controller. Show
/// "press any button on your controller" rather than "no controller found".</para>
/// <para><see cref="IGamepadManager.PollInterval"/> is ignored here. The loop is paced by the
/// display's refresh, which is also how often the browser itself updates the snapshot, so a
/// different interval would either duplicate work or drop input.</para>
/// </remarks>
public class BlazorGamepadManager : AbstractGamepadManager, IAsyncDisposable
{
    readonly IJSRuntime jsRuntime;
    readonly Dictionary<int, string> byIndex = new();
    DotNetObjectReference<BlazorGamepadManager>? reference;
    IJSObjectReference? module;
    BlazorGamepadProbe? probe;


    /// <summary>Creates the manager.</summary>
    public BlazorGamepadManager(IJSRuntime jsRuntime, ILogger<BlazorGamepadManager> logger) : base(logger)
        => this.jsRuntime = jsRuntime;


    internal async Task<IJSObjectReference> GetModule()
    {
        this.module ??= await this.jsRuntime
            .InvokeAsync<IJSObjectReference>("import", "./_content/Shiny.Gamepad.Blazor/gamepad.js")
            .ConfigureAwait(false);

        return this.module;
    }


    /// <summary>
    /// Checks whether this browser has the Gamepad API at all.
    /// </summary>
    /// <remarks>
    /// Feature detection needs a JS round trip, so it cannot happen in a constructor or a
    /// synchronous property. <see cref="IGamepadManager.GetGamepads"/> runs it anyway as part of
    /// starting; call this directly only to show a "your browser cannot do this" message before
    /// asking the player for a controller.
    /// </remarks>
    public async Task<BlazorGamepadProbe> Probe(CancellationToken ct = default)
    {
        var module = await this.GetModule().ConfigureAwait(false);
        this.probe = await module.InvokeAsync<BlazorGamepadProbe>("probe", ct).ConfigureAwait(false);

        return this.probe;
    }


    protected override async Task OnStart(CancellationToken ct)
    {
        var module = await this.GetModule().ConfigureAwait(false);

        this.probe ??= await module.InvokeAsync<BlazorGamepadProbe>("probe", ct).ConfigureAwait(false);
        if (!this.probe.Supported)
            throw new GamepadException("This browser does not implement the Gamepad API");

        this.reference = DotNetObjectReference.Create(this);

        var existing = await module
            .InvokeAsync<BlazorGamepadInfo[]>("start", ct, this.reference)
            .ConfigureAwait(false);

        foreach (var info in existing)
            this.Track(info);

        this.Logger.WatchStarted(existing.Length);
    }


    /// <summary>Called from JavaScript when a controller becomes visible to the page.</summary>
    [JSInvokable]
    public void OnConnected(BlazorGamepadInfo info) => this.Track(info);


    /// <summary>Called from JavaScript when a controller goes away.</summary>
    [JSInvokable]
    public void OnDisconnected(int index)
    {
        lock (this.byIndex)
        {
            if (!this.byIndex.Remove(index, out var id))
                return;

            this.Remove(id);
        }
    }


    /// <summary>
    /// Called from JavaScript once per animation frame, with every controller whose state changed.
    /// </summary>
    /// <remarks>
    /// Batched rather than one call per controller: two players moving at once is one interop call
    /// per frame, not two.
    /// </remarks>
    [JSInvokable]
    public void OnStates(BlazorGamepadState[] states)
    {
        foreach (var state in states)
        {
            string? id;
            lock (this.byIndex)
            {
                if (!this.byIndex.TryGetValue(state.Index, out id))
                    continue;
            }

            (this.Find(id) as BlazorGamepad)?.Apply(state);
        }
    }


    void Track(BlazorGamepadInfo info)
    {
        if (!info.Connected)
            return;

        lock (this.byIndex)
        {
            if (this.byIndex.ContainsKey(info.Index))
                return;

            // the browser's id string names the model, not the unit, and two identical controllers
            // share it - so the slot goes in too, and PersistentId is never claimed here
            this.byIndex[info.Index] = $"browser-{info.Index}";
        }

        this.Add(new BlazorGamepad($"browser-{info.Index}", info, this.GetModule, this.Logger));
    }


    /// <summary>Stops the browser-side loop and releases the JS module.</summary>
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        try
        {
            if (this.module != null)
            {
                await this.module.InvokeVoidAsync("stop").ConfigureAwait(false);
                await this.module.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (JSDisconnectedException)
        {
            // the circuit or the page is already gone; there is nothing left to stop
        }

        this.module = null;
        this.reference?.Dispose();
        this.reference = null;
    }
}
