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
        static bool added = false;
        readonly Type delegateType;
        readonly bool requestPermissionOnStart;


        public GeofenceModule(Type delegateType, bool requestPermissionOnStart)
        {
            this.delegateType = delegateType;
            this.requestPermissionOnStart = requestPermissionOnStart;
        }


        public override void Register(IServiceCollection services)
        {
            if (added)
                return;

            added = true;
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