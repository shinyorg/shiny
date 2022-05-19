using Microsoft.Extensions.DependencyInjection;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPush(this IServiceCollection services)
    {
        return services;
    }
}
//    readonly Type pushManagerType;
//    readonly Type delegateType;


//    public PushModule(Type pushManagerType, Type delegateType)
//    {
//        this.pushManagerType = pushManagerType;
//        this.delegateType = delegateType;
//    }


//    public override void Register(IServiceCollection services)
//    {
//        services.AddChannelManager();
//#if XAMARIN_IOS
//        // TODO: can I hook these differently dynamically with selector?

//        //application:didReceiveRemoteNotification:fetchCompletionHandler:
//        if (AppleExtensions.HasAppDelegateHook("didReceiveRemoteNotification")) { }
//            //logger.LogWarning("[SHINY] AppDelegate.DidReceiveRemoteNotification is not hooked - background notifications will not work without this!");

//        //application:didRegisterForRemoteNotificationsWithDeviceToken:"
//        AppleExtensions.AssertAppDelegateHook("didRegisterForRemoteNotificationsWithDeviceToken", "[SHINY] AppDelegate.RegisteredForRemoteNotifications is not hooked. This is a necessary hook for Shiny Push");

//        //application: didFailToRegisterForRemoteNotificationsWithError
//        AppleExtensions.AssertAppDelegateHook("didFailToRegisterForRemoteNotificationsWithError", "[SHINY] AppDelegate.FailedToRegisterForRemoteNotifications is not hooked. This is a necessary hook for Shiny Push");

//#endif

//        services.AddSingleton(typeof(IPushDelegate), this.delegateType);
//        services.TryAddSingleton(typeof(IPushManager), this.pushManagerType);
//        services.TryAddSingleton<PushContainer>();
//        services.TryAddSingleton<INativeAdapter, NativeAdapter>();
//    }
