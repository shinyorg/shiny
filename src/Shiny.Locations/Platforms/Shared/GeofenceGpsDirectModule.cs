#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace Shiny.Locations
{
    public class GeofenceGpsDirectModule : ShinyModule
    {
        readonly Type delegateType;
        public GeofenceGpsDirectModule(Type delegateType) => this.delegateType = delegateType;


        public override void Register(IServiceCollection services)
        {
            if (this.delegateType != null)
                services.AddSingleton(typeof(IGeofenceDelegate), this.delegateType);

            services.TryAddSingleton<IGeofenceManager, GpsGeofenceManagerImpl>();
            services.UseGps<GpsGeofenceDelegate>();
        }
    }
}
#endif