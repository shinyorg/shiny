#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
#if __ANDROID__
using Android.App;
using Android.Gms.Common;
#endif


namespace Shiny.Locations
{
    class GeofenceModule : ShinyModule
    {
        readonly Type delegateType;
        public GeofenceModule(Type delegateType) => this.delegateType = delegateType;


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
    }
}
#endif