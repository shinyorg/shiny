using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Locations;
using Shiny.Settings;
using Shiny.TripTracker;
using Shiny.TripTracker.Internals;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        static bool added = false;


        internal static int? CurrentTripId(this ISettings settings)
            => settings.Get<int?>(nameof(CurrentTripId));


        internal static void CurrentTripId(this ISettings settings, int? value)
            => settings.Set(nameof(CurrentTripId), value);


        public static bool UseTripTracker<T>(this IServiceCollection services) where T : ITripTrackerDelegate
            => services.UseTripTracker(typeof(T));


        public static bool UseTripTracker(this IServiceCollection services, Type delegateType)
        {            
            if (!services.UseMotionActivity())
                return false;

            if (!services.UseGps())
                return false;

            if (delegateType == null)
                throw new ArgumentException("Trip Tracker Delegate Type not supplied", nameof(delegateType));

            if (!added)
            {
                services.AddSingleton<IGpsDelegate, TripTrackerGpsDelegate>();
                services.AddSingleton<IDataService, SqliteDataService>();
                services.AddSingleton<ITripTrackerManager, TripTrackerManagerImpl>();
                added = true;
            }
            services.AddSingleton(typeof(ITripTrackerDelegate), delegateType);

            return true;
        }
    }
}
