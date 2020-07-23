using System;
using System.Threading.Tasks;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Shiny.BluetoothLE;
using Shiny.Infrastructure;
using Shiny;
using System.Collections.Generic;
using Shiny.Beacons;

namespace Shiny.Beacons
{
    public class BeaconMonitoringManager : IBeaconMonitoringManager
    {
        readonly IRepository repository;
        readonly IBleManager bleManager;
        readonly IMessageBus messageBus;
        IObservable<Beacon>? beaconScanner;


        public BeaconMonitoringManager(IBleManager bleManager,
                                       IMessageBus messageBus,
                                       IRepository repository)
            {
            this.bleManager = bleManager;
            this.messageBus = messageBus;
            this.repository = repository;
        }


        public AccessState GetCurrentStatus(bool background) => this.bleManager.Status;
        public Task<AccessState> RequestAccess(bool monitoring) => this.bleManager.RequestAccess().ToTask();
        public IObservable<AccessState> WhenAccessStatusChanged(bool monitoring) => this.bleManager.WhenStatusChanged();


        public async Task StartMonitoring(BeaconRegion region)
        {
            var stored = await this.repository.Set(region.Identifier, region);
            var eventType = stored ? BeaconRegisterEventType.Add : BeaconRegisterEventType.Update;
            this.messageBus.Publish(new BeaconRegisterEvent(eventType, region));
        }


        public async Task StopMonitoring(string identifier)
        {
            var region = await this.repository.Get<BeaconRegion>(identifier);
            if (region != null)
            {
                await this.repository.Remove<BeaconRegion>(identifier);
                this.messageBus.Publish(new BeaconRegisterEvent(BeaconRegisterEventType.Remove, region));
            }
        }

        public async Task StopAllMonitoring()
        {
            await this.repository.Clear<BeaconRegion>();
            this.messageBus.Publish(new BeaconRegisterEvent(BeaconRegisterEventType.Clear));
        }


        public Task<AccessState> RequestAccess() => this.bleManager.RequestAccess().ToTask();
        public async Task<IEnumerable<BeaconRegion>> GetMonitoredRegions() => await this.repository.GetAll<BeaconRegion>();
    }
}

