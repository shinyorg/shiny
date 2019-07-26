using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Android.Gms.Location;
using Android;
using Android.App;
using Android.Content;
using Shiny.Infrastructure;
using Shiny.Logging;


namespace Shiny.Locations
{
    public class GeofenceManagerImpl : AbstractGeofenceManager,
                                       IShinyStartupTask
    {
        readonly AndroidContext context;
        readonly GeofencingClient client;
        PendingIntent geofencePendingIntent;


        public GeofenceManagerImpl(AndroidContext context,
                                   IRepository repository) : base(repository)
        {
            this.context = context;
            this.client = LocationServices.GetGeofencingClient(this.context.AppContext);
            //mGoogleApiClient = new GoogleApiClient.Builder(this)
            //        .addConnectionCallbacks(this)
            //        .addOnConnectionFailedListener(this)
            //        .addApi(LocationServices.API)
            //        .build();
            //this.client.Connect();
        }


        public async void Start()
        {
            try
            {
                var regions = await this.Repository.GetAll();
                foreach (var region in regions)
                    await this.Create(region);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }


        public override IObservable<AccessState> WhenAccessStatusChanged() => Observable.Return(AccessState.Available);
        public override AccessState Status => this.context.GetCurrentAccessState(Manifest.Permission.AccessFineLocation);
        public override Task<AccessState> RequestAccess() => this.context.RequestAccess(Manifest.Permission.AccessFineLocation).ToTask();


        public override async Task StartMonitoring(GeofenceRegion region)
        {
            var access = await this.RequestAccess();
            access.Assert();

            await this.Create(region);
            await this.Repository.Set(region.Identifier, region);
        }


        public override async Task StopMonitoring(GeofenceRegion region)
        {
            await this.Repository.Remove(region.Identifier);
            await this.client.RemoveGeofencesAsync(new List<string> { region.Identifier });
        }


        public override async Task StopAllMonitoring()
        {
            var regions = await this.Repository.GetAll();
            var regionIds = regions.Select(x => x.Identifier).ToArray();
            await this.client.RemoveGeofencesAsync(regionIds);
            await this.Repository.Clear();
        }


        public override async Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken)
        {
            var client = LocationServices.GetFusedLocationProviderClient(this.context.AppContext);
            var location = await client.GetLastLocationAsync();
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
                .AddGeofence(geofence)
                .SetInitialTrigger(transitions)
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
                i = 1;

            if (region.NotifyOnExit)
                i += 2;

            return i;
        }


        protected virtual PendingIntent GetPendingIntent()
        {
            if (this.geofencePendingIntent != null)
                return this.geofencePendingIntent;

            var intent = new Intent(this.context.AppContext, typeof(GeofenceBroadcastReceiver));
            intent.SetAction(GeofenceBroadcastReceiver.INTENT_ACTION);
            //intent.SetAction(Permission.ReceiveBootCompleted);
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
