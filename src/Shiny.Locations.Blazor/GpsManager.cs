using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Shiny.Web.Infrastructure;

namespace Shiny.Locations.Blazor;


public class GpsManager : IGpsManager, IAsyncDisposable
{
    readonly Subject<GpsReading> readingSubj = new();
    readonly IJSInProcessRuntime jsRuntime;


    public GpsManager(IJSRuntime runtime)
    {
        this.jsRuntime = (IJSInProcessRuntime)runtime;
    }


    IJSInProcessObjectReference? jsRef;
    async Task<IJSInProcessObjectReference> GetModule()
    {
        this.jsRef ??= await this.jsRuntime.ImportInProcess("Shiny.Gps.Web", "gps.js");
        return this.jsRef;
    }


    public string Title { get; set; }
    public string Message { get; set; }
    public int Progress { get; set; }
    public int Total { get; set; }
    public bool IsIndeterministic { get; set; }
    public event PropertyChangedEventHandler PropertyChanged;


    public GpsRequest CurrentListener => throw new NotImplementedException();



    public IObservable<GpsReading> GetLastReading() => Observable.FromAsync<GpsReading>(async ct =>
    {
        var mod = await this.GetModule();
        var result = await mod.InvokeAsync<GeoPosition>("getCurrent"); // promise
        var pos = To(result);
        return pos;
    });


    public async Task<AccessState> RequestAccess(GpsRequest request)
    {
        var mod = await this.GetModule();
        var result = await mod.InvokeAsync<string>("requestAccess"); // promise
        return Utils.ToAccessState(result);
    }


    CompositeDisposable? disposer;

    public async Task StartListener(GpsRequest request)
    {
        //this.CurrentListener = request;
        this.disposer = new();
        var module = await this.GetModule();
        var watch = JsCallback<GeoPosition>.CreateInterop();
        this.disposer.Add(this.disposer);

        watch
            .Value
            .WhenResult()
            .Finally(() => module.InvokeVoid("stopListener"))
            .Subscribe(
                x => this.readingSubj.OnNext(To(x)),
                ex => this.readingSubj.OnError(ex)
            )
            .DisposedBy(this.disposer);

        module.InvokeVoid("startListener", watch);
    }


    public Task StopListener()
    {
        this.disposer?.Dispose();
        return Task.CompletedTask;
    }


    //public IObservable<AccessState> WhenAccessStatusChanged(GpsRequest request) => this
    //    .GetModule()
    //    .ToObservable()
    //    .Select(mod => Observable.Create<AccessState>(ob =>
    //    {
    //        var watch = JsCallback<string>.CreateInterop();
    //        var sub = watch.Value.WhenResult().Select(Utils.ToAccessState).Subscribe(state => ob.OnNext(state));

    //        mod.InvokeVoidAsync("shinyGps.whenStatusChanged", watch);
    //        return () =>
    //        {
    //            watch?.Dispose();
    //            sub?.Dispose();
    //        };
    //    }))
    //    .Switch();


    public IObservable<GpsReading> WhenReading() => this.readingSubj;

    public async ValueTask DisposeAsync()
    {
        if (this.jsRef != null)
            await this.jsRef.DisposeAsync();
    }


    static GpsReading To(GeoPosition pos) => new GpsReading(
        new Position(
            pos.Latitude,
            pos.Longitude
        ),
        pos.RawAccuracy ?? -99,
        pos.Timestamp,
        pos.RawHeading ?? -99,
        -1,
        pos.RawAltitude ?? 0,
        pos.RawSpeed ?? 0,
        -1
    );
}
