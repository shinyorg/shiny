using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Push;
using Shiny.Push.AzureNotificationHubs;
using Shiny.Push.AzureNotificationHubs.Infrastructure;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UsePushAzureNotificationHubs(this IServiceCollection services,
                                                        Type delegateType,
                                                        string listenerConnectionString,
                                                        string hubName)
        {
#if NETSTANDARD2_0
            return false;
#else
            services.TryAddSingleton<INativeAdapter, NativeAdapter>();
            services.UseNotifications();
            services.RegisterModule(new PushModule(
                typeof(Shiny.Push.AzureNotificationHubs.PushManager),
                delegateType
            ));
            services.AddSingleton(new AzureNotificationConfig(listenerConnectionString, hubName));
            return true;
#endif
        }


        public static bool UsePushAzureNotificationHubs<TPushDelegate>(this IServiceCollection services,
                                                                       string listenerConnectionString,
                                                                       string hubName)
            where TPushDelegate : class, IPushDelegate
            => services.UsePushAzureNotificationHubs(
                typeof(TPushDelegate),
                listenerConnectionString,
                hubName
            );
    }
}
