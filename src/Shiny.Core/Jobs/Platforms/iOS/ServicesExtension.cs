using System;
using Microsoft.Extensions.DependencyInjection;
using UIKit;


namespace Shiny.Jobs
{
    public static class ServicesExtension
    {
        // = UIApplication.BackgroundFetchIntervalMinimum
        public static void ConfigureJobService(this IServiceCollection services, double backgroundFetchInterval)
        {
            UIApplication
                .SharedApplication
                .SetMinimumBackgroundFetchInterval(backgroundFetchInterval);

            JobManager.BackgroundFetchInterval = backgroundFetchInterval;
        }


        public static void ConfigureJobService(this IServiceCollection services, TimeSpan timeSpan)
            => services.ConfigureJobService(timeSpan.TotalSeconds);
    }
}
