using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Notifications;
using Shiny.Push;
using Shiny.Push.Infrastructure;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPush(this IServiceCollection services, Type pushManagerType, Type delegateType)
    {
#if IOS || MACCATALYST
        // TODO: can I hook these differently dynamically with selector?
        // application:didReceiveRemoteNotification:"
        AppleExtensions.AssertAppDelegateHook(
            "didReceiveRemoteNotification",
            "[SHINY] AppDelegate.DidReceiveRemoteNotification is not hooked - background notifications will not work without this!"
        );

        // application:didRegisterForRemoteNotificationsWithDeviceToken:"
        AppleExtensions.AssertAppDelegateHook(
            "didRegisterForRemoteNotificationsWithDeviceToken",
            "[SHINY] AppDelegate.RegisteredForRemoteNotifications is not hooked. This is a necessary hook for Shiny Push"
        );

        //application: didFailToRegisterForRemoteNotificationsWithError
        AppleExtensions.AssertAppDelegateHook(
            "didFailToRegisterForRemoteNotificationsWithError",
            "[SHINY] AppDelegate.FailedToRegisterForRemoteNotifications is not hooked. This is a necessary hook for Shiny Push"
        );
#endif

#if IOS || MACCATALYST || ANDROID
        services.AddChannelManager();
        services.AddShinyService(typeof(IPushDelegate), delegateType);
        services.TryAddSingleton(typeof(IPushManager), pushManagerType);
        services.AddShinyServiceWithLifecycle<PushContainer>();
        services.AddShinyServiceWithLifecycle<INativeAdapter, NativeAdapter>();
#endif

        return services;
    }
}