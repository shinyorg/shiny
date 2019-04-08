using System;
using UIKit;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;


namespace Shiny
{
    public static class iOSExtensions
    {
        // = UIApplication.BackgroundFetchIntervalMinimum
        public static void ConfigureJobs(this IServiceCollection services, double backgroundFetchInterval)
        {
            UIApplication
                .SharedApplication
                .SetMinimumBackgroundFetchInterval(backgroundFetchInterval);

            JobManagerImpl.BackgroundFetchInterval = backgroundFetchInterval;
        }
    }
}
