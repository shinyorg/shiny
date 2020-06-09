﻿using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shiny.Notifications;
using Shiny.Push;
using Shiny.Push.AzureNotificationHubs;
using Shiny.Push.AzureNotifications;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UsePushAzureNotificationHubs(this IServiceCollection services,
                                                        Type delegateType,
                                                        string listenerConnectionString,
                                                        string hubName,
                                                        params NotificationCategory[] categories)
        {
#if NETSTANDARD2_0
            return false;
#else
#if __ANDROID__
            services.AddSingleton<IPushDelegate, AndroidPushDelegate>();
#endif
            services.RegisterModule(new PushModule(
                typeof(Shiny.Integrations.AzureNotifications.PushManager),
                delegateType,
                categories
            ));
            services.AddSingleton(new AzureNotificationConfig(listenerConnectionString, hubName));
            return true;
#endif
        }


        public static bool UsePushAzureNotificationHubs<TPushDelegate>(this IServiceCollection services,
                                                                       string listenerConnectionString,
                                                                       string hubName,
                                                                       params NotificationCategory[] categories)
            where TPushDelegate : class, IPushDelegate
            => services.UsePushAzureNotificationHubs(
                typeof(TPushDelegate),
                listenerConnectionString,
                hubName,
                categories
            );
    }
}
