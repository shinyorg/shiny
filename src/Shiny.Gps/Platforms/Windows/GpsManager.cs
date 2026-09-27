using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Windows.Devices.Geolocation;

namespace Shiny.Locations;


public class GpsManager(
    IServiceProvider services,
    ILogger<IGpsManager> logger
) : IGpsManager
{
    Geolocator? geolocator;


    public event EventHandler<GpsReading>? GpsReadingReceived;
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


    public async Task<GpsReading?> GetLastReading(TimeSpan? timeout = null)
    {
        using var cts = timeout.HasValue
            ? new CancellationTokenSource(timeout.Value)
            : new CancellationTokenSource();

        var loc = new Geolocator();
        var position = await loc.GetGeopositionAsync().AsTask(cts.Token).ConfigureAwait(false);
        return ToReading(position);
    }


    public async Task StartListener(GpsRequest request)
    {
        if (this.CurrentListener != null)
            throw new InvalidOperationException("GPS listener is already running");

        if (request.BackgroundMode != GpsBackgroundMode.None)
            throw new InvalidOperationException("Background GPS is not supported on Windows");

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
                this.GpsReadingReceived?.Invoke(this, reading);
                await services
                    .RunDelegates<IGpsDelegate>(x => x.OnReading(reading), logger)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing GPS reading");
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
