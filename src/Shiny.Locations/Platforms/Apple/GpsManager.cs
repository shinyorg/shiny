using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CoreFoundation;
using CoreLocation;
using Microsoft.Extensions.Logging;

namespace Shiny.Locations;


public class GpsManager : IGpsManager, IShinyStartupTask
{
    CLBackgroundActivitySession? session;
    CLLocationUpdater? updater;
    readonly ILogger logger;
    readonly Lazy<IEnumerable<IGpsDelegate>> delegates;
    
    public GpsManager(IServiceProvider services, ILogger<IGpsManager> logger)
    {
        this.delegates = services.GetLazyService<IEnumerable<IGpsDelegate>>();
        this.logger = logger;
    }
    
    
    public GpsRequest? CurrentListener { get; set; }
    public AccessState GetCurrentStatus(GpsRequest request) => AccessState.Unknown;


    public Task<AccessState> RequestAccess(GpsRequest request)
    {
        var bg = request.BackgroundMode != GpsBackgroundMode.None;
        var req = bg
            ? CLServiceSessionAuthorizationRequirement.Always
            : CLServiceSessionAuthorizationRequirement.WhenInUse;

        CLServiceSession ps = null!;
        var tcs = new TaskCompletionSource<AccessState>();
        
        try
        {
            // TODO: full accuracy?
            ps = CLServiceSession.CreateSession(req, DispatchQueue.CurrentQueue, diag =>
            {
                if (diag.AuthorizationRequestInProgress)
                    return;

                if (bg)
                {
                    if (!diag.AlwaysAuthorizationDenied)
                        tcs.SetResult(AccessState.Available);
                    else if (!diag.AuthorizationDenied)
                        tcs.SetResult(AccessState.Restricted);
                    else
                        tcs.SetResult(AccessState.Denied);
                }
                else
                {
                    if (!diag.AuthorizationDenied)
                        tcs.SetResult(AccessState.Available);
                    else
                        tcs.SetResult(AccessState.Denied);
                }
            });
        }
        finally
        {
            ps.Dispose();
        }

        return tcs.Task;
    }


    public IObservable<GpsReading?> GetLastReading() => Observable.Empty<GpsReading?>();
        // .FromAsync(() => this.RequestAccess(GpsRequest.Foreground))
        // .Do(x =>
        // {
        //     if (x != AccessState.Available)
        //         throw new InvalidOperationException("Invalid Permissions - " + x);
        // })
    // {
    //     
    //     CLLocationUpdater.CreateLiveUpdates(new DispatchQueue("shiny_gps"), async update =>
    //     {
    //         
    //     });
    //     return Observable.Empty<GpsReading>();
    // }

    
    readonly Subject<GpsReading> readSubject = new();
    public IObservable<GpsReading> WhenReading() => this.readSubject;

    
    public Task StartListener(GpsRequest request)
    {
        if (this.updater != null)
            throw new InvalidOperationException("Already GPS listener running");
        
        if (request.BackgroundMode != GpsBackgroundMode.None)
            this.session = CLBackgroundActivitySession.Create();
        
        this.updater = CLLocationUpdater.CreateLiveUpdates(new DispatchQueue("shiny_gps"), async update =>
        {
            var reading = update.Location.FromNative();
            this.readSubject.OnNext(reading); // safety?
            await this.delegates
                .Value
                .RunDelegates(
                    x => x.OnReading(reading),
                    this.logger
                )
                .ConfigureAwait(false);
        });
        this.CurrentListener = request;
        return Task.CompletedTask;
    }

    
    public Task StopListener()
    {
        this.CurrentListener = null;
        
        this.updater?.Dispose();
        this.updater = null;
        
        this.session?.Dispose();
        this.session = null;
        
        return Task.CompletedTask;
    }
    

    public async void Start()
    {
        if (this.CurrentListener == null)
            return;
        
        try
        {
            await this.StartListener(this.CurrentListener!);
        }
        catch (Exception ex)
        {
            this.logger.LogInformation(ex, "Failed to restart GPS listener");
        }
    }
}