#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;


namespace Shiny.Locations
{
    public class GeofenceGpsDirectModule : ShinyModule
    {
        readonly Type delegateType;
        readonly bool requestPermissionOnStart;


        public GeofenceGpsDirectModule(Type delegateType, bool requestPermissionOnStart)
        {
            this.delegateType = delegateType;
            this.requestPermissionOnStart = requestPermissionOnStart;
        }


        public override void Register(IServiceCollection services)
        {
            if (this.delegateType != null)
                services.AddSingleton(typeof(IGeofenceDelegate), this.delegateType);

            services.TryAddSingleton<IGeofenceManager, GpsGeofenceManagerImpl>();
            services.UseGps<GpsGeofenceDelegate>();
        }


        public override async void OnContainerReady(IServiceProvider services)
        {
            if (this.requestPermissionOnStart)
            {
                var access = await services
                    .GetRequiredService<IGeofenceManager>()
                    .RequestAccess();

                if (access != AccessState.Available)
                    services.Resolve<ILogger<IGeofenceManager>>().LogWarning("Invalid access - " + access);
            }
        }
    }
}
#endif