#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
#if __ANDROID__
using Android.App;
using Android.Gms.Common;
#endif


namespace Shiny.Locations
{
    class GeofenceModule : ShinyModule
    {
        readonly Type delegateType;
        readonly bool requestPermissionOnStart;


        public GeofenceModule(Type delegateType, bool requestPermissionOnStart)
        {
            this.delegateType = delegateType;
            this.requestPermissionOnStart = requestPermissionOnStart;
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
                services.TryAddSingleton<GeofenceProcessor>();
                services.TryAddSingleton(typeof(IGeofenceDelegate), this.delegateType);
                services.TryAddSingleton<IGeofenceManager, GeofenceManagerImpl>();
            }

#elif WINDOWS_UWP
            services.TryAddSingleton<IBackgroundTaskProcessor, GeofenceBackgroundTaskProcessor>();
            services.TryAddSingleton<IGeofenceManager, GeofenceManagerImpl>();
#else
            services.TryAddSingleton<IGeofenceManager, GeofenceManagerImpl>();
#endif
            // always add the delegate
            services.AddSingleton(typeof(IGeofenceDelegate), this.delegateType);
        }


        public override async void OnContainerReady(IServiceProvider services)
        {
            base.OnContainerReady(services);
            if (!this.requestPermissionOnStart)
                return;

            var logger = ShinyHost.LoggerFactory.CreateLogger<ILogger<IGeofenceManager>>();
            try
            {
                var mgr = services.GetService<IGeofenceManager>();
                var access = await mgr.RequestAccess();
                if (access != AccessState.Available)
                    logger.LogWarning("Location permissions denied on startup");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on startup geofence permission check");
            }
        }
    }
}
#endif