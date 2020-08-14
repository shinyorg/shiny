using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;
using Shiny.BluetoothLE;
using Shiny.Infrastructure;


namespace Shiny.Beacons
{
    public class BeaconMonitoringManager : IBeaconMonitoringManager
    {
        readonly IRepository repository;
        readonly IBleManager bleManager;
        readonly IMessageBus messageBus;
#if __ANDROID__
        readonly AndroidContext context;
#endif

        public BeaconMonitoringManager(IBleManager bleManager,
#if __ANDROID__
                                       AndroidContext context,
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


        public async Task StartMonitoring(BeaconRegion region)
        {
            var stored = await this.repository.Set(region.Identifier, region);
            var eventType = stored ? BeaconRegisterEventType.Add : BeaconRegisterEventType.Update;
            this.messageBus.Publish(new BeaconRegisterEvent(eventType, region));
#if __ANDROID__
            this.context.StartService(typeof(ShinyBeaconMonitoringService), true);
#endif
        }


        public async Task StopMonitoring(string identifier)
        {
            var region = await this.repository.Get<BeaconRegion>(identifier);
            if (region != null)
            {
                await this.repository.Remove<BeaconRegion>(identifier);
                this.messageBus.Publish(new BeaconRegisterEvent(BeaconRegisterEventType.Remove, region));

#if __ANDROID__
                var regions = await this.repository.GetAll<BeaconRegion>();
                if (regions.Count == 0)
                    this.context.StopService(typeof(ShinyBeaconMonitoringService));
#endif
            }
        }


        public async Task StopAllMonitoring()
        {
            await this.repository.Clear<BeaconRegion>();
            this.messageBus.Publish(new BeaconRegisterEvent(BeaconRegisterEventType.Clear));

#if __ANDROID__
            this.context.StopService(typeof(ShinyBeaconMonitoringService));
#endif
        }


        public Task<AccessState> RequestAccess() => this.bleManager.RequestAccess().ToTask();
        public async Task<IEnumerable<BeaconRegion>> GetMonitoredRegions() => await this.repository.GetAll<BeaconRegion>();
    }
}

