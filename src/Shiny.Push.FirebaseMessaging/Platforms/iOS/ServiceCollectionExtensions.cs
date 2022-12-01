//using System;
//using Microsoft.Extensions.DependencyInjection;
//using Shiny.Push;

//namespace Shiny;


//public static class ServiceCollectionExtensions
//{
//    public static IServiceCollection AddFirebaseMessaging<TPushDelegate>(this IServiceCollection services, FirebaseConfiguration? config = null) where TPushDelegate : class, IPushDelegate
//        => services.AddFirebaseMessaging(typeof(TPushDelegate), config);


//    public static IServiceCollection AddFirebaseMessaging(this IServiceCollection services, Type delegateType, FirebaseConfiguration? config = null)
//    {
//#if IOS
//        services.AddSingleton(config ?? new(true));
//        services.AddPush(typeof(Shiny.Push.FirebaseMessaging.PushManager), delegateType);

//#elif ANDROID

//        if (config == null || config.UseEmbeddedConfiguration)
//        {
//            services.AddPush(delegateType, FirebaseConfig.Embedded);
//        }
//        else
//        {
//            services.AddPush(
//                delegateType,
//                FirebaseConfig.FromValues(
//                    config.AppId,
//                    config.SenderId,
//                    config.ProjectId,
//                    config.ApiKey
//                )
//            );
//        }
//#endif

//        return services;
//    }
//}
