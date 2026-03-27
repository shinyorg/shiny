using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Windows.Devices.Geolocation;

namespace Shiny.Locations;


public class GpsManager : IGpsManager
{
    readonly Subject<GpsReading> readSubject = new();
    readonly IServiceProvider services;
    readonly ILogger logger;
    Geolocator? geolocator;


    public GpsManager(
        IServiceProvider services,
        ILogger<IGpsManager> logger
    )
    {
        this.services = services;
        this.logger = logger;
    }


    public GpsRequest? CurrentListener { get; private set; }


    public AccessState GetCurrentStatus(GpsRequest request)
    {
        var status = Geolocator.RequestAccessAsync().AsTask().GetAwaiter().GetResult();
        return FromNative(status);
    }


    public async Task<AccessState> RequestAccess(GpsRequest request)
    {
        if (request.BackgroundMode != GpsBackgroundMode.None)
            return AccessState.NotSupported;

        var status = await Geolocator.RequestAccessAsync();
        return FromNative(status);
    }


    public IObservable<GpsReading?> GetLastReading() => Observable.FromAsync<GpsReading?>(async ct =>
    {
        var loc = new Geolocator();
        var position = await loc.GetGeopositionAsync().AsTask(ct).ConfigureAwait(false);
        return ToReading(position);
    });


    public IObservable<GpsReading> WhenReading() => this.readSubject;


    public async Task StartListener(GpsRequest request)
    {
        if (this.CurrentListener != null)
            throw new InvalidOperationException("GPS listener is already running");

        if (request.BackgroundMode != GpsBackgroundMode.None)
            throw new InvalidOperationException("Background GPS is not supported on Windows");

        (await this.RequestAccess(request).ConfigureAwait(false)).Assert();

        this.geolocator = new Geolocator();

        if (request.RequestPreciseAccuracy)
            this.geolocator.DesiredAccuracy = PositionAccuracy.High;
        else
            this.geolocator.DesiredAccuracy = PositionAccuracy.Default;

        this.geolocator.PositionChanged += this.OnPositionChanged;
        this.CurrentListener = request;
    }


    public Task StopListener()
    {
        if (this.geolocator != null)
        {
            this.geolocator.PositionChanged -= this.OnPositionChanged;
            this.geolocator = null;
        }
        this.CurrentListener = null;
        return Task.CompletedTask;
    }


    async void OnPositionChanged(Geolocator sender, PositionChangedEventArgs args)
    {
        try
        {
            var reading = ToReading(args.Position);
            if (reading != null)
            {
                this.readSubject.OnNext(reading);
                await this.services
                    .RunDelegates<IGpsDelegate>(x => x.OnReading(reading), this.logger)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error processing GPS reading");
        }
    }


    static GpsReading? ToReading(Geoposition? position)
    {
        if (position?.Coordinate == null)
            return null;

        var coord = position.Coordinate;
        return new GpsReading(
            new Position(coord.Point.Position.Latitude, coord.Point.Position.Longitude),
            coord.Accuracy,
            coord.Timestamp,
            coord.Heading ?? 0,
            0,
            coord.Point.Position.Altitude,
            coord.Speed ?? 0,
            0
        );
    }


    static AccessState FromNative(GeolocationAccessStatus status) => status switch
    {
        GeolocationAccessStatus.Allowed => AccessState.Available,
        GeolocationAccessStatus.Denied => AccessState.Denied,
        _ => AccessState.Unknown
    };
}
