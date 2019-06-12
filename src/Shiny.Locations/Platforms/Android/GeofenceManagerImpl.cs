using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Android.Gms.Extensions;
using Android.Gms.Location;
using Android;
using Android.App;
using Android.Content;
using Shiny.Infrastructure;
using static Android.Manifest;


namespace Shiny.Locations
{
    public class GeofenceManagerImpl : AbstractGeofenceManager, IAndroidGeofenceManager
    {
        readonly AndroidContext context;
        readonly GeofencingClient client;
        readonly IGeofenceDelegate geofenceDelegate;


        public GeofenceManagerImpl(AndroidContext context,
                                   IRepository repository,
                                   IGeofenceDelegate geofenceDelegate) : base(repository)
        {
            this.context = context;
            this.client = LocationServices.GetGeofencingClient(this.context.AppContext);
            this.geofenceDelegate = geofenceDelegate;
        }


        public async Task ReceiveBoot()
        {
            var regions = await this.Repository.GetAll();
            foreach (var region in regions)
                await this.Create(region);
        }


        public async Task Process(Intent intent)
        {
            var e = GeofencingEvent.FromIntent(intent);
            if (e == null)
                return;

            foreach (var triggeringGeofence in e.TriggeringGeofences)
            {
                var region = await this.Repository.Get(triggeringGeofence.RequestId);
                if (region != null)
                {
                    var state = (GeofenceState)e.GeofenceTransition;
                    await this.geofenceDelegate.OnStatusChanged(state, region);

                    if (region.SingleUse)
                        await this.StopMonitoring(region);
                }
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
            await this.client.RemoveGeofences(new List<string> { region.Identifier });
        }


        public override async Task StopAllMonitoring()
        {
            var regions = await this.Repository.GetAll();
            var regionIds = regions.Select(x => x.Identifier).ToArray();
            await this.client.RemoveGeofences(regionIds);
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

            await this.client.AddGeofences(
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
            var intent = new Intent(this.context.AppContext, typeof(GeofenceBroadcastReceiver));
            intent.SetAction(GeofenceBroadcastReceiver.INTENT_ACTION);
            intent.SetAction(Permission.ReceiveBootCompleted);
            return PendingIntent.GetBroadcast(
                this.context.AppContext,
                0,
                intent,
                PendingIntentFlags.UpdateCurrent
            );
        }
    }
}
