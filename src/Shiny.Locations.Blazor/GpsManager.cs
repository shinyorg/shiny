using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Shiny.Locations.Blazor;


public class GpsManager(
    IJSRuntime jsRuntime,
    IServiceProvider services,
    ILogger<GpsManager> logger
) : IGpsManager, IAsyncDisposable
{
    IJSObjectReference? module;
    DotNetObjectReference<GpsManager>? selfRef;

    public GpsRequest? CurrentListener { get; private set; }

    public event EventHandler<GpsReading>? GpsReadingReceived;


    public AccessState GetCurrentStatus(GpsRequest request) => AccessState.Unknown;


    public async Task<AccessState> RequestAccess(GpsRequest request)
    {
        if (request.BackgroundMode != GpsBackgroundMode.None)
        {
            logger.LogWarning("Background GPS is not supported on Blazor WebAssembly. Request will be treated as foreground only.");
        }

        var mod = await this.GetModule().ConfigureAwait(false);
        var state = await mod.InvokeAsync<string>("requestAccess").ConfigureAwait(false);
        return MapPermission(state);
    }


    public async Task<GpsReading?> GetLastReading(TimeSpan? timeout = null)
    {
        using var cts = timeout.HasValue
            ? new CancellationTokenSource(timeout.Value)
            : new CancellationTokenSource();

        var mod = await this.GetModule().ConfigureAwait(false);
        var pos = await mod.InvokeAsync<GeoPosition?>("getCurrent", cts.Token).ConfigureAwait(false);
        return pos == null ? null : ToReading(pos);
    }


    public async Task StartListener(GpsRequest request)
    {
        if (this.CurrentListener != null)
            return;

        if (request.BackgroundMode != GpsBackgroundMode.None)
            logger.LogWarning("Background GPS is not supported on Blazor WebAssembly. Listener will only run while the page is active.");

        var mod = await this.GetModule().ConfigureAwait(false);
        this.selfRef ??= DotNetObjectReference.Create(this);

        await mod
            .InvokeVoidAsync("startListener", this.selfRef, request.RequestPreciseAccuracy)
            .ConfigureAwait(false);

        this.CurrentListener = request;
    }


    public async Task StopListener()
    {
        if (this.CurrentListener == null)
            return;

        var mod = await this.GetModule().ConfigureAwait(false);
        await mod.InvokeVoidAsync("stopListener").ConfigureAwait(false);
        this.CurrentListener = null;
    }


    [JSInvokable]
    public async Task OnReading(GeoPosition pos)
    {
        var reading = ToReading(pos);
        this.GpsReadingReceived?.Invoke(this, reading);
        await services
            .RunDelegates<IGpsDelegate>(x => x.OnReading(reading), logger)
            .ConfigureAwait(false);
    }


    [JSInvokable]
    public void OnError(string error)
        => logger.LogWarning("GPS listener error: {Error}", error);


    async Task<IJSObjectReference> GetModule()
    {
        this.module ??= await jsRuntime
            .InvokeAsync<IJSObjectReference>("import", "./_content/Shiny.Locations.Blazor/gps.js")
            .ConfigureAwait(false);

        return this.module;
    }


    static GpsReading ToReading(GeoPosition pos) => new(
        new Position(pos.Latitude, pos.Longitude),
        pos.RawAccuracy ?? 0,
        DateTimeOffset.FromUnixTimeMilliseconds(pos.Epoch),
        pos.RawHeading ?? 0,
        -1,
        pos.RawAltitude ?? 0,
        pos.RawSpeed ?? 0,
        -1
    );


    static AccessState MapPermission(string state) => state switch
    {
        "granted" => AccessState.Available,
        "denied" => AccessState.Denied,
        "prompt" => AccessState.Unknown,
        "notsupported" => AccessState.NotSupported,
        _ => AccessState.Unknown
    };


    public async ValueTask DisposeAsync()
    {
        this.selfRef?.Dispose();
        this.selfRef = null;

        if (this.module != null)
        {
            try
            {
                await this.module.InvokeVoidAsync("stopListener").ConfigureAwait(false);
            }
            catch { }

            await this.module.DisposeAsync().ConfigureAwait(false);
            this.module = null;
        }
    }
}
