﻿using Microsoft.Extensions.DependencyInjection;
using Shiny.Push;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPushFirebaseMessaging(this IServiceCollection services, FirebaseConfiguration? config = null)
    {
#if IOS
        services.AddSingleton(config ?? new(true));
        services.AddShinyService<FirebasePushProvider>();
        services.AddPush();
#endif
#if ANDROID
        if (config == null || config.UseEmbeddedConfiguration)
        {
            services.AddPush(FirebaseConfig.Embedded);
        }
        else
        {
            services.AddPush(new FirebaseConfig(
                false,
                config.AppId,
                config.SenderId,
                config.ProjectId,
                config.ApiKey,
                config.DefaultChannel,
                config.IntentAction
            ));
        }
#endif
        return services;
    }


    public static IServiceCollection AddPushFirebaseMessaging<TPushDelegate>(this IServiceCollection services, FirebaseConfiguration? config = null)
         where TPushDelegate : class, IPushDelegate
    {
        services.AddShinyService<TPushDelegate>();
        services.AddPushFirebaseMessaging(config);
        return services;
    }
}