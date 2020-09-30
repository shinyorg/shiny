using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Settings;
using Shiny.TripTracker;
using Shiny.TripTracker.Internals;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        static bool added = false;


        internal static int? CurrentTripId(this ISettings settings)
            => settings.Get<int?>(nameof(CurrentTripId), null);


        internal static void CurrentTripId(this ISettings settings, int? value)
            => settings.Set(nameof(CurrentTripId), value);


        public static bool UseTripTracker<T>(this IServiceCollection services, TripTrackingType? startupTracking = null) where T : ITripTrackerDelegate
            => services.UseTripTracker(typeof(T));


        public static bool UseTripTracker(this IServiceCollection services, Type delegateType, TripTrackingType? startupTracking = null)
        {
            if (!services.UseMotionActivity())
                return false;

            if (!services.UseGps())
                return false;

            if (delegateType == null)
                throw new ArgumentException("Trip Tracker Delegate Type not supplied", nameof(delegateType));

            services.RegisterModule(new TripTrackerModule(delegateType, startupTracking));
            return true;
        }
    }
}
