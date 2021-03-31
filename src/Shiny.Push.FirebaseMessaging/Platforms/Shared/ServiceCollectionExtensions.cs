using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications;
using Shiny.Push;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseFirebaseMessaging<TPushDelegate>(this IServiceCollection services, params Channel[] channels) where TPushDelegate : class, IPushDelegate
            => services.UseFirebaseMessaging(typeof(TPushDelegate), channels);


        public static bool UseFirebaseMessaging(this IServiceCollection services, Type delegateType, params Channel[] channels)
        {
#if __IOS__
            services.RegisterModule(new PushModule(
                typeof(Shiny.Push.FirebaseMessaging.PushManager),
                delegateType,
                channels
            ));
            return true;
#elif __ANDROID__
            return services.UsePush(delegateType, channels);
#else
            return false;
#endif
        }
    }
}
