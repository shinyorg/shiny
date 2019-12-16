#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Logging;

namespace Shiny.Locations
{
    class GeofenceModule : ShinyModule
    {
        readonly GeofenceRegion[] startingRegions;
        readonly Type delegateType;


        public GeofenceModule(Type delegateType, GeofenceRegion[] regions)
        {
            this.startingRegions = regions;
            this.delegateType = delegateType;
        }


        public override void Register(IServiceCollection services)
        {
#if __ANDROID__
            services.AddSingleton<GeofenceProcessor>();
#elif WINDOWS_UWP
            services.AddSingleton<IBackgroundTaskProcessor, GeofenceBackgroundTaskProcessor>();
#endif
            services.AddSingleton(typeof(IGeofenceDelegate), this.delegateType);
            services.AddSingleton<IGeofenceManager, GeofenceManagerImpl>();
        }


        public override async void OnContainerReady(IServiceProvider services)
        {
            base.OnContainerReady(services);
            if (this.startingRegions.IsEmpty())
                return;

            try
            {
                var mgr = services.GetService<IGeofenceManager>();
                var access = await mgr.RequestAccess();
                if (access == AccessState.Available)
                    foreach (var region in this.startingRegions)
                        await mgr.StartMonitoring(region);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}
#endif