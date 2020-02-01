using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Locations
{
    class GpsGeofenceDelegate : IGpsDelegate
    {
        readonly IGeofenceManager geofenceManager;
        readonly IGeofenceDelegate geofenceDelegate;
        

        public GpsGeofenceDelegate(IGeofenceManager geofenceManager, IGeofenceDelegate geofenceDelegate)
        {
            this.geofenceManager = geofenceManager;
            this.geofenceDelegate = geofenceDelegate;
        }


        public async Task OnReading(IGpsReading reading)
        {
            // TODO: need previous state
            var geofences = await this.geofenceManager.GetMonitorRegions();
            foreach (var geofence in geofences)
            {
                var state = geofence.IsPositionInside(reading.Position)
                    ? GeofenceState.Entered
                    : GeofenceState.Exited;

                await this.geofenceDelegate.OnStatusChanged(state, geofence);
            }
        }
    }


    public class GpsGeofenceManagerImpl : AbstractGeofenceManager
    {
        static readonly GpsRequest Request = new GpsRequest { UseBackground = true };


        readonly IGpsManager? gpsManager;
        public GpsGeofenceManagerImpl(IRepository repository, IGpsManager? gpsManager = null) : base(repository)
            => this.gpsManager = gpsManager;


        public override AccessState Status => this.gpsManager?.GetCurrentStatus(Request) ?? AccessState.NotSupported;
        public override async Task<AccessState> RequestAccess()
        {
            if (this.gpsManager == null)
                return AccessState.NotSupported;

            return await this.gpsManager.RequestAccess(new GpsRequest { UseBackground = true });
        }


        public override async Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default)
        {
            this.Assert();

            var reading = await this.gpsManager
                .GetLastReading()
                .Timeout(TimeSpan.FromSeconds(10))
                .ToTask();

            if (reading == null)
                return GeofenceState.Unknown;

            var state = region.IsPositionInside(reading.Position)
                ? GeofenceState.Entered
                : GeofenceState.Exited;

            return state;
        }

        public override async Task StartMonitoring(GeofenceRegion region)
        {
            this.Assert();
            await this.Repository.Set(region.Identifier, region);

            if (!this.gpsManager.IsListening)
            {
                await this.gpsManager.StartListener(new GpsRequest
                {
                    Interval = TimeSpan.FromMinutes(1),
                    UseBackground = true
                });
            }
        }

        public override async Task StopAllMonitoring()
        {
            this.Assert();
            await this.Repository.Clear();
            await this.gpsManager.StopListener();
        }


        public override async Task StopMonitoring(string identifier)
        {
            this.Assert();
            await this.Repository.Remove(identifier);
            var geofences = await this.Repository.GetAll();

            if (geofences.Count == 0)
                await this.gpsManager.StopListener();
        }


        public override IObservable<AccessState> WhenAccessStatusChanged()
        {
            this.Assert();
            return this.gpsManager.WhenAccessStatusChanged(Request);
        }


        void Assert()
        {
            if (this.gpsManager == null)
                throw new ArgumentException("GPS Manager is not available");

            this.gpsManager.GetCurrentStatus(Request).Assert();
        }
    }
}