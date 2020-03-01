#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Logging;
#if __ANDROID__
using Android.App;
using Android.Gms.Common;
#endif

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
            var resultCode = GoogleApiAvailability
                .Instance
                .IsGooglePlayServicesAvailable(Application.Context);

            if (resultCode == ConnectionResult.ServiceMissing)
            {
                services.UseGpsDirectGeofencing(this.delegateType);
            }
            else
            {
                services.AddSingleton<GeofenceProcessor>();
                services.AddSingleton(typeof(IGeofenceDelegate), this.delegateType);
                services.AddSingleton<IGeofenceManager, GeofenceManagerImpl>();
            }
            
#elif WINDOWS_UWP
            services.AddSingleton<IBackgroundTaskProcessor, GeofenceBackgroundTaskProcessor>();
            services.AddSingleton(typeof(IGeofenceDelegate), this.delegateType);
            services.AddSingleton<IGeofenceManager, GeofenceManagerImpl>();
#else
            services.AddSingleton(typeof(IGeofenceDelegate), this.delegateType);
            services.AddSingleton<IGeofenceManager, GeofenceManagerImpl>();
#endif

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