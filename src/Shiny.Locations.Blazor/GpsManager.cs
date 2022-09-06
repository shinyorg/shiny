using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Shiny.Infrastructure;
using Shiny.Web.Infrastructure;

namespace Shiny.Locations.Blazor;


public class GpsManager : IGpsManager, IShinyWebAssemblyService
{
    readonly Subject<GpsReading> readingSubj = new();
    IJSInProcessObjectReference module = null!;


    public async Task OnStart(IJSInProcessRuntime jsRuntime)
    {
        this.module = await jsRuntime.ImportInProcess("Shiny.Locations.Blazor", "gps.js");
    }


    public string Title { get; set; }
    public string Message { get; set; }
    public int Progress { get; set; }
    public int Total { get; set; }
    public bool IsIndeterministic { get; set; }
    public event PropertyChangedEventHandler PropertyChanged;
    public GpsRequest CurrentListener { get; private set; } // TODO: restore


    public IObservable<GpsReading> GetLastReading() => Observable.FromAsync<GpsReading>(async ct =>
    {
        (await this.RequestAccess(GpsRequest.Foreground)).Assert();

        var result = await this.module.InvokeAsync<GeoPosition>("getCurrent"); // promise
        var pos = To(result);
        return pos;
    });


    public Task<AccessState> RequestAccess(GpsRequest request) => this.module.RequestAccess();


    CompositeDisposable? disposer;

    public async Task StartListener(GpsRequest request)
    {
        // TODO: if already listening
        (await this.RequestAccess(request)).Assert();

        this.CurrentListener = request;
        this.disposer = new();
        var watch = JsCallback<GeoPosition>.CreateInterop();
        this.disposer.Add(this.disposer);

        watch
            .Value
            .WhenResult()
            .Finally(() => this.module.InvokeVoid("stopListener"))
            .Subscribe(
                x => this.readingSubj.OnNext(To(x)),
                ex => this.readingSubj.OnError(ex)
            )
            .DisposedBy(this.disposer);

        this.module.InvokeVoid("startListener", watch);
    }


    public Task StopListener()
    {
        this.disposer?.Dispose();
        return Task.CompletedTask;
    }


    public IObservable<GpsReading> WhenReading() => this.readingSubj;


    static GpsReading To(GeoPosition pos) => new GpsReading(
        new Position(
            pos.Latitude,
            pos.Longitude
        ),
        pos.RawAccuracy ?? 0,
        pos.Timestamp,
        pos.RawHeading ?? 0,
        -1,
        pos.RawAltitude ?? 0,
        pos.RawSpeed ?? 0,
        -1
    );
}
