using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Locations.Infrastructure;


namespace Shiny.Locations
{
    public class GpsGeofenceManagerImpl : AbstractGeofenceManager, IShinyStartupTask
    {
        static readonly GpsRequest Request = new GpsRequest { UseBackground = true };
        readonly IGpsManager? gpsManager;


        public GpsGeofenceManagerImpl(IRepository repository, IGpsManager? gpsManager = null) : base(repository)
            => this.gpsManager = gpsManager;


        public async void Start()
        {
            var restore = await this.GetMonitorRegions();
            if (restore.Any())
                this.TryStartGps();
        }


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
            this.TryStartGps();
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


        protected async void TryStartGps()
        {
            if (this.gpsManager.CurrentListener == null)
            {
                await this.gpsManager.StartListener(new GpsRequest
                {
                    Interval = TimeSpan.FromMinutes(1),
                    UseBackground = true
                });
            }
        }


        protected void Assert()
        {
            if (this.gpsManager == null)
                throw new ArgumentException("GPS Manager is not available");

            this.gpsManager.GetCurrentStatus(Request).Assert();
        }
    }
}