using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Push;
#if __IOS__
using Shiny.Notifications;
#endif

namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UsePush<TDelegate>(this IServiceCollection services, bool requestAccessOnStart = false) where TDelegate : class, IPushDelegate
            => services.UsePush(typeof(TDelegate), requestAccessOnStart);


        public static bool UsePush(this IServiceCollection services, Type? delegateType = null, bool requestAccessOnStart = false)
        {
#if NETSTANDARD
            return false;
#else

#if __IOS__
            services.RegisterIosNotificationContext();
#endif
            services.AddSingleton<IPushManager, PushManager>();
            if (delegateType != null)
                services.AddSingleton(typeof(IPushDelegate), delegateType);

            if (requestAccessOnStart)
            {
                services.RegisterPostBuildAction(async sp =>
                    await sp.Resolve<IPushManager>().RequestAccess()
                );
            }
            return true;
#endif
        }
    }
}
