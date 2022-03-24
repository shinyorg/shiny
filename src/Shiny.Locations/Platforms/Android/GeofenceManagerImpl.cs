using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Android.App;
using Android.Content;
using Android.Gms.Location;
using Shiny.Infrastructure;
using Shiny.Locations.Infrastructure;
using Microsoft.Extensions.Logging;


namespace Shiny.Locations
{
    public class GeofenceManagerImpl : AbstractGeofenceManager, IShinyStartupTask
    {
        public const string ReceiverName = "com.shiny.locations." + nameof(GeofenceBroadcastReceiver);
        public const string IntentAction = ReceiverName + ".INTENT_ACTION";
        readonly IPlatform context;
        readonly GeofencingClient client;
        readonly IServiceProvider services;
        readonly ILogger logger;
        PendingIntent? geofencePendingIntent;


        public GeofenceManagerImpl(IPlatform context,
                                   IRepository repository,
                                   IServiceProvider services,
                                   ILogger<GeofenceManagerImpl> logger) : base(repository)
        {
            this.context = context;
            this.logger = logger;
            this.services = services;
            this.client = LocationServices.GetGeofencingClient(this.context.AppContext);
        }


        public async void Start()
        {
            GeofenceBroadcastReceiver.Process = async e =>
            {
                if (e.HasError)
                {
                    var err = GeofenceStatusCodes.GetStatusCodeString(e.ErrorCode);
                    this.logger.LogWarning("Geofence OS error - " + err);
                }
                else if (e.TriggeringGeofences != null)
                {
                    foreach (var triggeringGeofence in e.TriggeringGeofences)
                    {
                        var state = (GeofenceState)e.GeofenceTransition;
                        var region = await this.Repository.Get(triggeringGeofence.RequestId);

                        if (region == null)
                        {
                            this.logger.LogWarning("Geofence reported by OS not found in Shiny Repository - RequestID: " + triggeringGeofence.RequestId);
                        }
                        else
                        {
                            await this.services
                                .RunDelegates<IGeofenceDelegate>(
                                    x => x.OnStatusChanged(state, region),
                                    ex => this.logger.LogError($"Error in geofence delegate - Region: {region.Identifier} State: {state}")
                                )
                                .ConfigureAwait(false);
                        }
                    }
                }
            };
            var regions = await this.Repository.GetAll();
            foreach (var region in regions)
                await this.Create(region);
        }


        //public override AccessState Status
        //    => this.context.GetCurrentLocationAccess(true, true, true, true);

        public override Task<AccessState> RequestAccess()
            => this.context.RequestBackgroundLocationAccess(LocationPermissionType.FineRequired);


        public override async Task StartMonitoring(GeofenceRegion region)
        {
            (await this.RequestAccess().ConfigureAwait(false)).Assert();
            await this.Create(region).ConfigureAwait(false);
            await this.Repository.Set(region.Identifier, region).ConfigureAwait(false);
        }


        public override async Task StopMonitoring(string identifier)
        {
            await this.Repository.Remove(identifier).ConfigureAwait(false);
            await this.client.RemoveGeofencesAsync(new List<string> { identifier }).ConfigureAwait(false);
        }


        public override async Task StopAllMonitoring()
        {
            var regions = await this.Repository.GetAll().ConfigureAwait(false);
            var regionIds = regions.Select(x => x.Identifier).ToArray();
            if (regionIds.Any())
                await this.client.RemoveGeofencesAsync(regionIds).ConfigureAwait(false);

            await this.Repository.Clear().ConfigureAwait(false);
        }


        public override async Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken)
        {
            var location = await LocationServices
                .GetFusedLocationProviderClient(this.context.AppContext)
                .GetLastLocationAsync()
                .ConfigureAwait(false);

            if (location == null)
                return GeofenceState.Unknown;

            var inside = region.IsPositionInside(new Position(location.Latitude, location.Longitude));
            var state = inside ? GeofenceState.Entered : GeofenceState.Exited;
            return state;
        }


        protected virtual async Task Create(GeofenceRegion region)
        {
            var transitions = this.GetTransitions(region);

            var geofence = new GeofenceBuilder()
                .SetRequestId(region.Identifier)
                .SetExpirationDuration(Geofence.NeverExpire)
                .SetCircularRegion(
                    region.Center.Latitude,
                    region.Center.Longitude,
                    Convert.ToSingle(region.Radius.TotalMeters)
                )
                .SetTransitionTypes(transitions)
                .Build();

            var request = new GeofencingRequest.Builder()
                .SetInitialTrigger(0)
                .AddGeofence(geofence)
                .Build();

            await this.client
                .AddGeofencesAsync(
                    request,
                    this.GetPendingIntent()
                )
                .ConfigureAwait(false);
        }


        protected virtual int GetTransitions(GeofenceRegion region)
        {
            var i = 0;
            if (region.NotifyOnEntry)
                i += Geofence.GeofenceTransitionEnter;

            if (region.NotifyOnExit)
                i += Geofence.GeofenceTransitionExit;

            return i;
        }


        protected virtual PendingIntent GetPendingIntent() 
            => this.geofencePendingIntent ??= this.context.GetBroadcastPendingIntent<GeofenceBroadcastReceiver>(IntentAction, PendingIntentFlags.UpdateCurrent);
    }
}
