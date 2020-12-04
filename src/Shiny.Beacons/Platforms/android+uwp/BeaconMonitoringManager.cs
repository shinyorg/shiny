using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;
using Shiny.BluetoothLE;
using Shiny.Infrastructure;


namespace Shiny.Beacons
{
#if __ANDROID__
    public class BeaconMonitoringManager : NotifyPropertyChanged, IBeaconMonitoringManager, IBeaconMonitoringNotificationConfiguration, IShinyStartupTask
#else
    public class BeaconMonitoringManager : IBeaconMonitoringManager, IShinyStartupTask
#endif
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

#if __ANDROID__

        string? title;
        public string? Title
        {
            get => this.title;
            set => this.Set(ref this.title, value);
        }


        string? description;
        public string? Description
        {
            get => this.description;
            set => this.Set(ref this.description, value);
        }


        string? ticker;
        public string? Ticker
        {
            get => this.ticker;
            set => this.Set(ref this.ticker, value);
        }

#endif

        public async void Start()
        {
#if __ANDROID__
            var regions = await this.GetMonitoredRegions();
            if (!regions.IsEmpty() && !ShinyBeaconMonitoringService.IsStarted)
                this.context.StartService(typeof(ShinyBeaconMonitoringService), true);
#else
            // TODO: start backgroundtask manually?
#endif
        }


        public async Task StartMonitoring(BeaconRegion region)
        {
            var stored = await this.repository.Set(region.Identifier, region);
            var eventType = stored ? BeaconRegisterEventType.Add : BeaconRegisterEventType.Update;
            this.messageBus.Publish(new BeaconRegisterEvent(eventType, region));
#if __ANDROID__
            if (!ShinyBeaconMonitoringService.IsStarted)
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


        public async Task<AccessState> RequestAccess()
        {
            var access = await this.bleManager.RequestAccess().ToTask();
#if __ANDROID__
            if (access == AccessState.Available && this.context.IsMinApiLevel(26))
                access = await this.context.RequestAccess(Android.Manifest.Permission.ForegroundService).ToTask();
#endif
            return access;
        }


        public async Task<IEnumerable<BeaconRegion>> GetMonitoredRegions() => await this.repository.GetAll<BeaconRegion>();
    }
}

