using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Push;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseFirebaseMessaging<TPushDelegate>(this IServiceCollection services, FirebaseConfiguration? config = null) where TPushDelegate : class, IPushDelegate
            => services.UseFirebaseMessaging(typeof(TPushDelegate), config);


        public static bool UseFirebaseMessaging(this IServiceCollection services, Type delegateType, FirebaseConfiguration? config = null)
        {
#if XAMARINIOS
            services.AddSingleton(config ?? new FirebaseConfiguration {  UseEmbeddedConfiguration = true });

            services.RegisterModule(new PushModule(
                typeof(Shiny.Push.FirebaseMessaging.PushManager),
                delegateType
            ));
            return true;
#elif MONOANDROID

            if (config == null)
            {
                services.UsePush(delegateType);
            }
            else
            {
                services.UsePush(
                    delegateType,
                    new FirebaseConfig
                    {
                        UseEmbeddedConfiguration = config.UseEmbeddedConfiguration,
                        AppId = config.AppId,
                        SenderId = config.SenderId,
                        ProjectId = config.ProjectId,
                        ApiKey = config.ApiKey
                    }
                );
            }
            return true;
#else
            return false;
#endif
        }
    }
}
