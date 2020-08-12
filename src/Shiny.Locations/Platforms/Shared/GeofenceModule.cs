#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Logging;
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
            services.TryAddSingleton(typeof(IGeofenceDelegate), this.delegateType);
            services.TryAddSingleton<IGeofenceManager, GeofenceManagerImpl>();
#else
            services.TryAddSingleton(typeof(IGeofenceDelegate), this.delegateType);
            services.TryAddSingleton<IGeofenceManager, GeofenceManagerImpl>();
#endif

        }


        public override async void OnContainerReady(IServiceProvider services)
        {
            base.OnContainerReady(services);
            if (!this.requestPermissionOnStart)
                return;

            try
            {
                var mgr = services.GetService<IGeofenceManager>();
                var access = await mgr.RequestAccess();
                if (access != AccessState.Available)
                    Log.Write("Geofence", "Permission Denied on startup");
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}
#endif