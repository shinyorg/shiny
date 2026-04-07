using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Shiny.Locations.Blazor;


public class GpsManager : IGpsManager, IAsyncDisposable
{
    readonly IJSRuntime jsRuntime;
    readonly IServiceProvider services;
    readonly ILogger logger;
    readonly Subject<GpsReading> readingSubj = new();

    IJSObjectReference? module;
    DotNetObjectReference<GpsManager>? selfRef;


    public GpsManager(IJSRuntime jsRuntime, IServiceProvider services, ILogger<GpsManager> logger)
    {
        this.jsRuntime = jsRuntime;
        this.services = services;
        this.logger = logger;
    }


    public GpsRequest? CurrentListener { get; private set; }


    public AccessState GetCurrentStatus(GpsRequest request) => AccessState.Unknown;


    public async Task<AccessState> RequestAccess(GpsRequest request)
    {
        if (request.BackgroundMode != GpsBackgroundMode.None)
        {
            this.logger.LogWarning("Background GPS is not supported on Blazor WebAssembly. Request will be treated as foreground only.");
        }

        var mod = await this.GetModule().ConfigureAwait(false);
        var state = await mod.InvokeAsync<string>("requestAccess").ConfigureAwait(false);
        return MapPermission(state);
    }


    public IObservable<GpsReading?> GetLastReading() => Observable.FromAsync<GpsReading?>(async () =>
    {
        var mod = await this.GetModule().ConfigureAwait(false);
        var pos = await mod.InvokeAsync<GeoPosition?>("getCurrent").ConfigureAwait(false);
        return pos == null ? null : ToReading(pos);
    });


    public IObservable<GpsReading> WhenReading() => this.readingSubj;


    public async Task StartListener(GpsRequest request)
    {
        if (this.CurrentListener != null)
            return;

        if (request.BackgroundMode != GpsBackgroundMode.None)
            this.logger.LogWarning("Background GPS is not supported on Blazor WebAssembly. Listener will only run while the page is active.");

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
        this.readingSubj.OnNext(reading);
        await this.services
            .RunDelegates<IGpsDelegate>(x => x.OnReading(reading), this.logger)
            .ConfigureAwait(false);
    }


    [JSInvokable]
    public void OnError(string error)
        => this.logger.LogWarning("GPS listener error: {Error}", error);


    async Task<IJSObjectReference> GetModule()
    {
        this.module ??= await this.jsRuntime
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
