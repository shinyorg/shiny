using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Android.App;
using Android.Gms.Location;
using Shiny.Infrastructure;
using Shiny.Logging;


namespace Shiny.Locations
{
    public class GeofenceManagerImpl : AbstractGeofenceManager, IShinyStartupTask
    {
        public const string ReceiverName = "com.shiny.locations." + nameof(GeofenceBroadcastReceiver);
        public const string IntentAction = ReceiverName + ".INTENT_ACTION";
        readonly AndroidContext context;
        readonly GeofencingClient client;
        PendingIntent? geofencePendingIntent;


        public GeofenceManagerImpl(AndroidContext context, IRepository repository) : base(repository)
        {
            this.context = context;
            this.client = LocationServices.GetGeofencingClient(this.context.AppContext);
        }


        public async void Start() => Log.SafeExecute(async () =>
        {
            var regions = await this.Repository.GetAll();
            foreach (var region in regions)
                await this.Create(region);
        });


        public override IObservable<AccessState> WhenAccessStatusChanged()
            => Observable.Interval(TimeSpan.FromSeconds(2)).Select(_ => this.Status);


        public override AccessState Status
            => this.context.GetCurrentLocationAccess(true, true, true, true);

        public override Task<AccessState> RequestAccess()
            => this.context.RequestLocationAccess(true, true, true, true);


        public override async Task StartMonitoring(GeofenceRegion region)
        {
            await this.Create(region);
            await this.Repository.Set(region.Identifier, region);
        }


        public override async Task StopMonitoring(string identifier)
        {
            await this.Repository.Remove(identifier);
            await this.client.RemoveGeofencesAsync(new List<string> { identifier });
        }


        public override async Task StopAllMonitoring()
        {
            var regions = await this.Repository.GetAll();
            var regionIds = regions.Select(x => x.Identifier).ToArray();
            if (regionIds.Any())
                await this.client.RemoveGeofencesAsync(regionIds);

            await this.Repository.Clear();
        }


        public override async Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken)
        {
            var location = await LocationServices
                .GetFusedLocationProviderClient(this.context.AppContext)
                .GetLastLocationAsync();

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

            await this.client.AddGeofencesAsync(
                request,
                this.GetPendingIntent()
            );
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
        {
            if (this.geofencePendingIntent != null)
                return this.geofencePendingIntent;

            var intent = this.context.CreateIntent<GeofenceBroadcastReceiver>(IntentAction);
            this.geofencePendingIntent = PendingIntent.GetBroadcast(
                this.context.AppContext,
                0,
                intent,
                PendingIntentFlags.UpdateCurrent
            );
            return this.geofencePendingIntent;
        }
    }
}
