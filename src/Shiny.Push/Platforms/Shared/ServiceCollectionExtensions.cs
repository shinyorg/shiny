using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications;
using Shiny.Push;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UsePush<TDelegate>(this IServiceCollection services) where TDelegate : class, IPushDelegate
            => services.UsePush(typeof(TDelegate));


        public static bool UsePush(this IServiceCollection services, Type delegateType)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new PushModule(
                typeof(PushManager),
                delegateType
            ));
            return true;
#endif
        }
    }
}
