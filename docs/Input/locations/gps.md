Title: GPS
---

The Global Position System (GPS) on Shiny is actually a bit more complicated than other Shiny modules as it can really "hammer" away on your resources if you set it up wrong.

<?! PackageInfo "Shiny.Locations" "Shiny.Locations.IGpsManager" /?>

```cs
using Microsoft.Extensions.DependencyInjection;

using Shiny;

public class GpsStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseGps();

        // OR

        services.UseGps<GpsDelegate>();
    }
}

```cs
using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Locations
{
    public interface IGpsDelegate : IShinyDelegate
    {
        /// <summary>
        /// This is fired when the gps reading has changed. 
        /// </summary>
        /// <param name="reading">The gps reading.</param>
        Task OnReading(IGpsReading reading);
    }
}

```

```cs
using System;
using System.Threading.Tasks;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class SyncGpsDelegate : NotifyPropertyChanged, IGpsDelegate
    {
        readonly IJobManager jobManager;
        readonly IMotionActivityManager activityManager;
        readonly IGpsDataService dataService;


        public SyncGpsDelegate(IJobManager jobManager,
                               IMotionActivityManager activityManager,
                               IGpsDataService dataService)
        {
            this.jobManager = jobManager;
            this.activityManager = activityManager;
            this.dataService = dataService;
        }


        public async Task OnReading(IGpsReading reading)
        {
            var job = await this.jobManager.GetJob(Constants.GpsJobIdentifier);
            if (job == null)
                return;

            var config = job.GetSyncConfig();
            MotionActivityEvent? activity = null;
            if (config.IncludeMotionActivityEvents)
                activity = await this.activityManager.GetCurrentActivity();

            var e = new GpsEvent
            {
                Id = Guid.NewGuid().ToString(),
                DateCreated = DateTimeOffset.UtcNow,

                Latitude = reading.Position.Latitude,
                Longitude = reading.Position.Longitude,
                Heading = reading.Heading,
                HeadingAccuracy = reading.HeadingAccuracy,
                Speed = reading.Speed,
                PositionAccuracy = reading.PositionAccuracy,
                Activities = activity?.Types
            };
            await this.dataService.Create(e);
            var batchSize = await this.dataService.GetPendingCount();

            if (batchSize >= config.BatchSize)
            {
                Console.WriteLine("GPS Location Sync batch size reached, will attempt to sync");
                if (!this.jobManager.IsRunning)
                    await this.jobManager.RunJobAsTask(Constants.GpsJobIdentifier);
            }
        }
    }
}

```

```cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Locations;
using Shiny.Settings;


namespace Shiny.TripTracker.Internals
{
    public class TripTrackerGpsDelegate : IGpsDelegate
    {
        readonly ITripTrackerManager manager;
        readonly IMotionActivityManager activityManager;
        readonly IDataService dataService;
        readonly ISettings settings;
        readonly IEnumerable<ITripTrackerDelegate> delegates;


        public TripTrackerGpsDelegate(ITripTrackerManager manager,
                                      IMotionActivityManager activityManager,
                                      IDataService dataService,
                                      ISettings settings,
                                      IEnumerable<ITripTrackerDelegate> delegates)
        {
            this.manager = manager;
            this.activityManager = activityManager;
            this.dataService = dataService;
            this.settings = settings;
            this.delegates = delegates;
        }


        int? CurrentTripId
        {
            get => this.settings.CurrentTripId();
            set => this.settings.CurrentTripId(value);
        }


        public async Task OnReading(IGpsReading reading)
        {
            if (this.manager.TrackingType == null)
                return;

            // TODO: watch for sensor blips?
            // TODO: watch for overlap
            var n = nameof(TripTrackerGpsDelegate);
            var currentMotion = await this.GetLastActivity();
            var track = this.IsTracked(currentMotion);
            Logging.Log.Write(n, $"Current Motion: {currentMotion?.Types.ToString() ?? "Empty"} - Track: {track} - Current: {this.CurrentTripId}");

            if (this.CurrentTripId == null)
            {
                if (!track)
                    return;

                var trip = new Trip
                {
                    StartLatitude = reading.Position.Latitude,
                    StartLongitude = reading.Position.Longitude,
                    Type = this.manager.TrackingType.Value,
                    DateStarted = DateTimeOffset.UtcNow
                };
                await this.dataService.Save(trip);
                this.settings.CurrentTripId(trip.Id);

                await this.dataService.Checkin(trip.Id, reading);
                await this.delegates.RunDelegates(x => x.OnTripStart(trip));
            }
            else
            {
                await this.dataService.Checkin(this.CurrentTripId.Value, reading);

                if (!track)
                {
                    // stop trip
                    var trip = await this.dataService.GetTrip(this.CurrentTripId.Value);
                    trip.DateFinished = DateTimeOffset.UtcNow;
                    trip.AverageSpeedMetersPerHour = await this.dataService.GetTripAverageSpeedInMetersPerHour(trip.Id);
                    trip.TotalDistanceMeters = await this.dataService.GetTripTotalDistanceInMeters(trip.Id);
                    trip.EndLongitude = reading.Position.Longitude;
                    trip.EndLatitude = reading.Position.Latitude;

                    this.settings.CurrentTripId(null);
                    await this.dataService.Save(trip);
                    await this.delegates.RunDelegates(x => x.OnTripEnd(trip));
                }
            }
        }


        async Task<MotionActivityEvent> GetLastActivity()
        {
            var ts = TimeSpan.FromMinutes(5);
            var currentTripId = this.settings.CurrentTripId();

            if (currentTripId != null)
            {
                var trip = await this.dataService.GetTrip(currentTripId.Value);
                ts = DateTimeOffset.UtcNow.Subtract(trip.DateStarted);
            }
            return await this.activityManager.GetCurrentActivity(ts);
        }


        bool IsTracked(MotionActivityEvent? e)
        {
            if (e == null)
                return this.settings.CurrentTripId() != null;

            switch (this.manager.TrackingType.Value)
            {
                case TripTrackingType.Stationary:
                    return e.Types.HasFlag(MotionActivityType.Stationary);

                case TripTrackingType.Cycling:
                    return e.Types.HasFlag(MotionActivityType.Cycling);

                case TripTrackingType.Running:
                    return e.Types.HasFlag(MotionActivityType.Running);

                case TripTrackingType.Walking:
                    return e.Types.HasFlag(MotionActivityType.Walking);

                case TripTrackingType.Automotive:
                    return e.Types.HasFlag(MotionActivityType.Automotive);

                case TripTrackingType.Exercise:
                    return e.Types.HasFlag(MotionActivityType.Cycling) ||
                           e.Types.HasFlag(MotionActivityType.Running) ||
                           e.Types.HasFlag(MotionActivityType.Walking);

                case TripTrackingType.OnFoot:
                    return e.Types.HasFlag(MotionActivityType.Running) ||
                           e.Types.HasFlag(MotionActivityType.Walking);

                default:
                    throw new Exception("Invalid Flag");
            }
        }
    }
}

```

```cs
using System.Threading.Tasks;
using Shiny.Locations;

public class GpsDelegate : IGpsDelegate
{
    public async Task OnReading(IGpsReading reading)
    {
    }
}
```

```cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Locations.Sync;

public class LocationSyncGpsDelegate : IGpsSyncDelegate
{
    public Task Process(IEnumerable<GpsEvent> gpsEvent, CancellationToken cancelToken)
    {
        throw new NotImplementedException();
    }
}
```

```cs
using System.Threading.Tasks;
using Shiny;
using Shiny.Locations;


public class GpsUsage
{
    public async Task Usage()
    {
        var manager = ShinyHost.Resolve<IGpsManager>();
        var result = await manager.RequestAccess(GpsRequest.Realtime(true));
        if (result == AccessState.Available)
        {
            //manager.WhenReading().Subscribe(reading =>
            //{

            //});
            await manager.StartListener(GpsRequest.Realtime(true));

            await manager.StopListener();
        }
    }
}
```
