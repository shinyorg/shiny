using System;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Integrations.FirebaseNotifications
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseFirebaseMessaging(this IServiceCollection services)
        {
#if XAMARIN_IOS
            services.AddSingleton<Shiny.Push.IPushManager, Shiny.Integrations.FirebaseNotifications.PushManager>();
            return true;
#elif __ANDROID__
            services.UsePush();
            return true;
#else
            return false;
#endif
        }
    }
}
