using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.TripTracker.Internals;


namespace Shiny.TripTracker
{
    public static class ServiceCollectionExtensions
    {
        public static void UseTripTracking(this IServiceCollection services, Type? delegateType = null)
        {
            services.UseMotionActivity();
            services.UseGps<TripTrackerGpsDelegate>();
            services.AddSingleton<IDataService, SqliteDataService>();
            services.AddSingleton<ITripTrackerManager, TripTrackerManagerImpl>();
        }
    }
}
