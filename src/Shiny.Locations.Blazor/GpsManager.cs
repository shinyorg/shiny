using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Shiny.Web.Infrastructure;

namespace Shiny.Locations.Web;


public class GpsManager : IGpsManager, IAsyncDisposable
{
    readonly Subject<GpsReading> readingSubj = new();
    readonly IJSRuntime js;


    public GpsManager(IJSRuntime js)
    {
        this.js = js;
    }


    IJSObjectReference? jsRef;
    async Task<IJSObjectReference> GetModule()
    {
        this.jsRef ??= await this.js.Import("Shiny.Gps.Web", "gps");
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
        var result = await mod.InvokeAsync<GeoPosition>("shinyGps.getCurrentGps");
        //return result;
        return default;
    });


    //public async Task<AccessState> RequestAccess(GpsRequest request)
    //{
    //    var mod = await this.GetModule();
    //    var result = await mod.InvokeAsync<string>("shinyGps.requestAccess");
    //    return Utils.ToAccessState(result);
    //}


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
            .Finally(() => module.InvokeVoidAsync("shinyGps.stopListener"))
            .Subscribe(
                this.readingSubj.OnNext,
                this.readingSubj.OnError
            )
            .DisposedBy(this.disposer);

        await module.InvokeVoidAsync("shinyGps.startListener", watch);
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
}
