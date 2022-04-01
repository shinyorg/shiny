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
#if __IOS__
            config?.AssertValid();
            if (config != null)
                services.AddSingleton(config);

            services.RegisterModule(new PushModule(
                typeof(Shiny.Push.FirebaseMessaging.PushManager),
                delegateType
            ));
            return true;
#elif __ANDROID__

            if (config != null)
            {
                services.UsePush(
                    delegateType,
                    new FirebaseConfig
                    { 
                        AppId = config.AppId,
                        SenderId = config.SenderId,
                        ProjectId = config.ProjectId,
                        ApiKey = config.ApiKey
                    }
                );
            }
            else
            {
                services.UsePush(delegateType);
            }
            return true;
#else
            return false;
#endif
        }
    }
}
