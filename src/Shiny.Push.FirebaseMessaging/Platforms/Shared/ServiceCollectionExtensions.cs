using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications;
using Shiny.Push;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseFirebaseMessaging<TPushDelegate>(this IServiceCollection services) where TPushDelegate : class, IPushDelegate
            => services.UseFirebaseMessaging(typeof(TPushDelegate));


        public static bool UseFirebaseMessaging(this IServiceCollection services, Type delegateType)
        {
#if __IOS__
            services.RegisterModule(new PushModule(
                typeof(Shiny.Push.FirebaseMessaging.PushManager),
                delegateType
            ));
            return true;
#elif __ANDROID__
            return services.UsePush(delegateType);
#else
            return false;
#endif
        }
    }
}
