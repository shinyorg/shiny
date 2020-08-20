using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications;
using Shiny.Push;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseFirebaseMessaging<TPushDelegate>(this IServiceCollection services, params NotificationCategory[] categories) where TPushDelegate : class, IPushDelegate
            => services.UseFirebaseMessaging(typeof(TPushDelegate), categories);


        public static bool UseFirebaseMessaging(this IServiceCollection services, Type delegateType, params NotificationCategory[] categories)
        {
#if XAMARIN_IOS
            services.RegisterModule(new PushModule(
                typeof(Shiny.Push.FirebaseMessaging.PushManager),
                delegateType,
                categories
            ));
            return true;
#elif __ANDROID__
            return services.UsePush(delegateType, categories);
#else
            return false;
#endif
        }
    }
}
