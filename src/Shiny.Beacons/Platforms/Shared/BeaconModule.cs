﻿#if !NETSTANDARD
using System;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Beacons
{
    class BeaconModule : ShinyModule
    {
        readonly BeaconRegion[] regionsToMonitorWhenPermissionAvailable;
        readonly Type delegateType;


        public BeaconModule(Type delegateType, BeaconRegion[] regionsToMonitorWhenPermissionAvailable)
        {
            this.delegateType = delegateType;
            this.regionsToMonitorWhenPermissionAvailable = regionsToMonitorWhenPermissionAvailable;
        }


        public override void Register(IServiceCollection services)
        {
            if (this.delegateType != null)
                services.AddSingleton(typeof(IBeaconDelegate), this.delegateType);

#if WINDOWS_UWP || __ANDROID__
            services.UseBleCentral();
            services.AddSingleton<BackgroundTask>();
#endif
            services.AddSingleton<IBeaconManager, BeaconManager>();
        }


        public override void OnContainerReady(IServiceProvider services)
        {
            base.OnContainerReady(services);
#if __ANDROID__
            services
                .Resolve<AndroidContext>(true)
                .StartService(typeof(BeaconService));
#endif

            if (this.regionsToMonitorWhenPermissionAvailable != null)
            {
                var mgr = services.GetService<IBeaconManager>();
                mgr
                    .WhenAccessStatusChanged(true)
                    .Where(x => x == AccessState.Available)
                    .Take(1)
                    .SubscribeAsync(async () =>
                    {
                        foreach (var region in regionsToMonitorWhenPermissionAvailable)
                            await mgr.StartMonitoring(region);
                    });
            }
        }
    }
}
#endif