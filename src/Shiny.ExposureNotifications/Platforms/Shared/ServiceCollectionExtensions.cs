using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.ExposureNotifications;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseExposureNotifications<TDelegate>(this IServiceCollection services) where TDelegate : IExposureNotificationDelegate
        {
#if __IOS__ || __ANDROID__
            services.AddSingleton<IExposureNotificationManager, ExposureNotificationManager>();
            services.RegisterJob(typeof(ExposureNotificationJob));
#endif
        }
    }
}
