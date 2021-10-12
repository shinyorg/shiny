Title: Geofencing
---

<?! PackageInfo "Shiny.Locations" "Shiny.Locations.IGeofenceManager" /?>


```cs
using Microsoft.Extensions.DependencyInjection;
using Shiny;

public class GeofenceStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services, IPlatform platform)
    {
        services.UseGeofencing<GeofenceDelegate>();
    }
}

```

```cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Locations
{
    public class GpsGeofenceDelegate : NotifyPropertyChanged, IGpsDelegate
    {
        readonly IGeofenceManager geofenceManager;
        readonly IGeofenceDelegate geofenceDelegate;
        public Dictionary<string, GeofenceState> CurrentStates { get; set; }


        public GpsGeofenceDelegate(IGeofenceManager geofenceManager, IGeofenceDelegate geofenceDelegate)
        {
            this.CurrentStates = new Dictionary<string, GeofenceState>();
            this.geofenceManager = geofenceManager;
            this.geofenceDelegate = geofenceDelegate;
        }


        public async Task OnReading(IGpsReading reading)
        {
            var geofences = await this.geofenceManager.GetMonitorRegions();
            foreach (var geofence in geofences)
            {
                var state = geofence.IsPositionInside(reading.Position)
                    ? GeofenceState.Entered
                    : GeofenceState.Exited;

                var current = this.GetState(geofence.Identifier);
                if (state != current)
                {
                    this.SetState(geofence.Identifier, state);
                    await this.geofenceDelegate.OnStatusChanged(state, geofence);
                }
            }
        }


        protected GeofenceState GetState(string geofenceId)
            => this.CurrentStates.ContainsKey(geofenceId)
                ? this.CurrentStates[geofenceId]
                : GeofenceState.Unknown;


        protected virtual void SetState(string geofenceId, GeofenceState state)
        {
            this.CurrentStates[geofenceId] = state;
            this.RaisePropertyChanged(nameof(this.CurrentStates));
        }
    }

}

```

```cs
using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Locations
{
    public interface IGeofenceDelegate : IShinyDelegate
    {
        /// <summary>
        /// This is fired when the geofence region status has changed
        /// </summary>
        /// <param name="newStatus">The new geofence state.</param>
        /// <param name="region">The geofence region.</param>
        Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region);
    }
}

```

```cs
using System;
using System.Threading.Tasks;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class SyncGeofenceDelegate : IGeofenceDelegate
    {
        readonly IJobManager jobManager;
        readonly IMotionActivityManager activityManager;
        readonly IGeofenceDataService dataService;


        public SyncGeofenceDelegate(IJobManager jobManager,
                                    IMotionActivityManager activityManager,
                                    IGeofenceDataService dataService)
        {
            this.jobManager = jobManager;
            this.activityManager = activityManager;
            this.dataService = dataService;
        }


        public async Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
        {
            var job = await this.jobManager.GetJob(Constants.GeofenceJobIdentifer);
            if (job == null)
                return;

            var config = job.GetSyncConfig();
            MotionActivityEvent? activity = null;
            if (config.IncludeMotionActivityEvents)
                activity = await this.activityManager.GetCurrentActivity();

            var e = new GeofenceEvent
            {
                Id = Guid.NewGuid().ToString(),
                DateCreated = DateTimeOffset.UtcNow,

                Identifier = region.Identifier,
                Entered = newStatus == GeofenceState.Entered,
                Activities = activity?.Types
            };
            await this.dataService.Create(e);
            if (!this.jobManager.IsRunning)
                await this.jobManager.RunJobAsTask(Constants.GeofenceJobIdentifer);
        }
    }
}

```

```cs
using System.Threading.Tasks;
using Shiny.Locations;

public class GeofenceDelegate : IGeofenceDelegate
{
    public async Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
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

public class LocationSyncGeofenceDelegate : IGeofenceSyncDelegate
{
    public Task Process(IEnumerable<GeofenceEvent> geofence, CancellationToken cancelToken)
    {
        throw new NotImplementedException();
    }
}
```

```cs
using System.Threading.Tasks;
using Shiny;
using Shiny.Locations;

public class GeofenceUsage
{
    public async Task Usage()
    {
        var manager = ShinyHost.Resolve<IGeofenceManager>();
        var result = await manager.RequestAccess();
        if (result == AccessState.Available)
        {
            await manager.StartMonitoring(new GeofenceRegion(
                "YourIdentifier",
                new Position(1, 1),
                Distance.FromKilometers(1)
            ));
        }
    }


    public async Task Stop()
    {
        await ShinyHost.Resolve<IGeofenceManager>().StopMonitoring("YourIdentifier");

        // or

        await ShinyHost.Resolve<IGeofenceManager>().StopAllMonitoring();
    }
}
```
