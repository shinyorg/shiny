using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Push;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseFirebaseMessaging<TPushDelegate>(this IServiceCollection services) where TPushDelegate : class, IPushDelegate
        {
#if XAMARIN_IOS
            services.AddSingleton<IPushManager, Shiny.Push.FirebaseMessaging.PushManager>();
            services.AddSingleton<IPushDelegate, TPushDelegate>();
            return true;
#elif __ANDROID__
            return services.UsePush<TPushDelegate>();
#else
            return false;
#endif
        }


        public static bool UseFirebaseMessaging(this IServiceCollection services, Type delegateType)
        {
#if XAMARIN_IOS
            services.AddSingleton<IPushManager, Shiny.Push.FirebaseMessaging.PushManager>();
            services.AddSingleton(typeof(IPushDelegate), delegateType);
            return true;
#elif __ANDROID__
            return services.UsePush(delegateType);
#else
            return false;
#endif
        }
    }
}
