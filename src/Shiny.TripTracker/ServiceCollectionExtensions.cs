using System;
using Microsoft.Extensions.DependencyInjection;
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

            if (!services.UseGps<TripTrackerGpsDelegate>())
                return false;

            services.AddSingleton<IDataService, SqliteDataService>();
            services.AddSingleton<ITripTrackerManager, TripTrackerManagerImpl>();
            if (delegateType != null)
                services.AddSingleton(typeof(ITripTrackerDelegate), delegateType);

            return true;
        }
    }
}
