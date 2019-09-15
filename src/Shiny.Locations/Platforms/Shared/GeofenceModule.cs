#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Logging;

namespace Shiny.Locations
{
    class GeofenceModule<T> : ShinyModule where T: class, IGeofenceDelegate
    {
        readonly GeofenceRegion[] startingRegions;
        public GeofenceModule(GeofenceRegion[] regions)
        {
            this.startingRegions = regions;
        }


        public override void Register(IServiceCollection services)
        {
#if __ANDROID__
            services.AddSingleton<GeofenceProcessor>();
#elif WINDOWS_UWP
            services.AddSingleton<IBackgroundTaskProcessor, GeofenceBackgroundTaskProcessor>();
#endif
            services.AddSingleton<IGeofenceDelegate, T>();
            services.AddSingleton<IGeofenceManager, GeofenceManagerImpl>();
        }


        public override async void OnContainerReady(IServiceProvider services)
        {
            base.OnContainerReady(services);
            if (this.startingRegions == null)
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