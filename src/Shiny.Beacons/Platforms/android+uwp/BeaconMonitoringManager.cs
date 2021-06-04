using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;
using Shiny.BluetoothLE;
using Shiny.Infrastructure;
#if MONOANDROID
using Shiny.Locations;
#endif

namespace Shiny.Beacons
{

    public partial class BeaconMonitoringManager : IBeaconMonitoringManager, IShinyStartupTask
    {
        readonly IRepository repository;
        readonly IBleManager bleManager;
        readonly IMessageBus messageBus;
#if __ANDROID__
        readonly IAndroidContext context;
#endif


        public BeaconMonitoringManager(IBleManager bleManager,
#if __ANDROID__
                                       IAndroidContext context,
#endif
                                       IMessageBus messageBus,
                                       IRepository repository)
        {
            this.bleManager = bleManager;
#if __ANDROID__
            this.context = context;
#endif
            this.messageBus = messageBus;
            this.repository = repository;
        }


        public async void Start()
        {
            var regions = await this.GetMonitoredRegions();
            if (!regions.IsEmpty())
                this.StartService();
        }


        public async Task StartMonitoring(BeaconRegion region)
        {
            var stored = await this.repository.Set(region.Identifier, region);
            var eventType = stored ? BeaconRegisterEventType.Add : BeaconRegisterEventType.Update;
            this.messageBus.Publish(new BeaconRegisterEvent(eventType, region));
            this.StartService();
        }


        public async Task StopMonitoring(string identifier)
        {
            var region = await this.repository.Get<BeaconRegion>(identifier);
            if (region != null)
            {
                await this.repository.Remove<BeaconRegion>(identifier);
                this.messageBus.Publish(new BeaconRegisterEvent(BeaconRegisterEventType.Remove, region));

                var regions = await this.repository.GetAll<BeaconRegion>();
                if (regions.Count == 0)
                    this.StopService();
            }
        }


        public async Task StopAllMonitoring()
        {
            await this.repository.Clear<BeaconRegion>();
            this.messageBus.Publish(new BeaconRegisterEvent(BeaconRegisterEventType.Clear));
            this.StopService();
        }


        public async Task<AccessState> RequestAccess()
        {
            var access = await this.bleManager.RequestAccess().ToTask();
#if MONOANDROID
            await this.context.RequestLocationAccess(true, true, true, false);
            if (access == AccessState.Available && this.context.IsMinApiLevel(26))
                access = await this.context.RequestAccess(Android.Manifest.Permission.ForegroundService).ToTask();
#endif
            return access;
        }


        public async Task<IEnumerable<BeaconRegion>> GetMonitoredRegions()
            => await this.repository.GetAll<BeaconRegion>();


        void StartService()
        {
#if MONOANDROID
            if (!ShinyBeaconMonitoringService.IsStarted)
                this.context.StartService(typeof(ShinyBeaconMonitoringService));
            // TODO: uwp
#endif
        }


        void StopService()
        {
#if MONOANDROID
            if (ShinyBeaconMonitoringService.IsStarted)
                this.context.StopService(typeof(ShinyBeaconMonitoringService));
// TODO: uwp
#endif
        }
    }
}

