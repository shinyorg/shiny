using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    /// <param name="services"></param>
    /// <param name="delegateType"></param>
    /// <param name="androidConfig">Android specific default configuration</param>
    /// <param name="channels">WARNING: This will replace all current channels with this set</param>
    /// <returns></returns>
    public static bool AddNotifications(this IServiceCollection services, Type? delegateType = null)
    {
#if IOS || MACCATALYST || ANDROID
        return true;
#else
        return false;
#endif
    }


#if ANDROID
    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    /// <typeparam name="TNotificationDelegate"></typeparam>
    /// <param name="services"></param>
    /// <param name="androidConfig">Android specific default configuration</param>
    /// <param name="channels">WARNING: This will replace all current channels with this set</param>
    /// <returns></returns>
    public static bool AddNotifications<TNotificationDelegate>(this IServiceCollection services,
                                                               AndroidOptions? androidConfig = null,
                                                               params Channel[] channels)
            where TNotificationDelegate : class, INotificationDelegate
        => services.UseNotifications(
            typeof(TNotificationDelegate),
            androidConfig,
            channels
        );


    /// <summary>
    /// Registers notification manager with Shiny
    /// </summary>
    /// <param name="services"></param>
    /// <param name="androidConfig">Android specific default configuration</param>
    /// <param name="channels">WARNING: This will replace all current channels with this set</param>
    /// <returns></returns>
    public static bool AddNotifications(this IServiceCollection services)
    {
        return true;
    }
#endif
}


//readonly Type? delegateType;
//readonly Channel[]? channels;


//public NotificationModule(Type? delegateType,
//                          AndroidOptions? androidConfig = null,
//                          Channel[]? channels = null)
//{
//    this.delegateType = delegateType;
//    this.channels = channels;

//    if (androidConfig != null)
//    {
//        AndroidOptions.DefaultLaunchActivityFlags = androidConfig.LaunchActivityFlags;
//        AndroidOptions.DefaultShowWhen = androidConfig.ShowWhen;
//        AndroidOptions.DefaultSmallIconResourceName = androidConfig.SmallIconResourceName ?? AndroidOptions.DefaultSmallIconResourceName;
//        AndroidOptions.DefaultColorResourceName = androidConfig.ColorResourceName;
//    }
//}


//public override void Register(IServiceCollection services)
//{
//    if (this.delegateType != null)
//        services.AddSingleton(typeof(INotificationDelegate), this.delegateType);

//    services.TryAddSingleton<INotificationManager, NotificationManager>();
//#if __ANDROID__ || __IOS__
//            services.AddChannelManager();
//#endif
//#if __ANDROID__
//            services.UseGeofencing<NotificationGeofenceDelegate>();
//            services.TryAddSingleton<AndroidNotificationProcessor>();
//            services.TryAddSingleton<AndroidNotificationManager>();
//#elif WINDOWS_UWP
//            services.RegisterJob(new Jobs.JobInfo(typeof(NotificationJob), runOnForeground: true));
//#endif
//}


//public override async void OnContainerReady(IServiceProvider services)
//{
//    base.OnContainerReady(services);
//    if (this.channels?.Any() ?? false)
//    {
//        var manager = services.GetRequiredService<INotificationManager>();
//        foreach (var channel in this.channels)
//            await manager.AddChannel(channel).ConfigureAwait(false);
//    }
//}