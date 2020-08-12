using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Locations;
using Shiny.TripTracker;
using Shiny.TripTracker.Internals;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseTripTracker<T>(this IServiceCollection services) where T : ITripTrackerDelegate
            => services.UseTripTracker(typeof(T));


        public static bool UseTripTracker(this IServiceCollection services, Type? delegateType = null)
        {            
            if (!services.UseMotionActivity())
                return false;

            if (!services.UseGps())
                return false;

            services.AddSingleton<IGpsDelegate, TripTrackerGpsDelegate>();
            services.TryAddSingleton<IDataService, SqliteDataService>();
            services.TryAddSingleton<ITripTrackerManager, TripTrackerManagerImpl>();
            if (delegateType != null)
                services.AddSingleton(typeof(ITripTrackerDelegate), delegateType);

            return true;
        }
    }
}
