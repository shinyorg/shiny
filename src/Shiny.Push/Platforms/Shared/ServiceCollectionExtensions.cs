using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications;
using Shiny.Push;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UsePush<TDelegate>(this IServiceCollection services,
                                              bool requestAccessOnStart = false,
                                              params NotificationCategory[] categories) where TDelegate : class, IPushDelegate
            => services.UsePush(typeof(TDelegate), requestAccessOnStart, categories);


        public static bool UsePush(this IServiceCollection services,
                                   Type delegateType,
                                   bool requestAccessOnStart = false,
                                   params NotificationCategory[] categories)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new PushModule(
                typeof(PushManager),
                delegateType,
                requestAccessOnStart
            ));
            return true;
#endif
        }
    }
}
