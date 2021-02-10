#if !NETSTANDARD
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace Shiny.Notifications
{
    class NotificationModule : ShinyModule
    {
        readonly Type? delegateType;
        readonly bool requestPermissionImmediately;
        readonly Channel[] channels;


        public NotificationModule(Type? delegateType,
                                  bool requestPermissionImmediately,
                                  AndroidOptions? androidConfig = null,
                                  Channel[]? channels = null)
        {
            this.delegateType = delegateType;
            this.requestPermissionImmediately = requestPermissionImmediately;
            this.channels = channels;

            if (androidConfig != null)
            {
                AndroidOptions.DefaultLaunchActivityFlags = androidConfig.LaunchActivityFlags;
                AndroidOptions.DefaultShowWhen = androidConfig.ShowWhen;
                AndroidOptions.DefaultSmallIconResourceName = androidConfig.SmallIconResourceName ?? AndroidOptions.DefaultSmallIconResourceName;
                AndroidOptions.DefaultColorResourceName = androidConfig.ColorResourceName;
            }
        }


        public override void Register(IServiceCollection services)
        {
            if (this.delegateType != null)
                services.AddSingleton(typeof(INotificationDelegate), this.delegateType);

            services.TryAddSingleton<INotificationManager, NotificationManager>();
#if __ANDROID__
            services.TryAddSingleton<AndroidNotificationProcessor>();
#elif __IOS__
            services.TryAddSingleton<iOSNotificationDelegate>();
#elif WINDOWS_UWP

            UwpPlatform.RegisterBackground<NotificationBackgroundTaskProcessor>(x => x.SetTrigger(
                new Windows.ApplicationModel.Background.UserNotificationChangedTrigger(
                    Windows.UI.Notifications.NotificationKinds.Toast
                )
            ));
#endif

#if __ANDROID__ || WINDOWS_UWP
            services.RegisterJob(typeof(NotificationJob));
            services.UseJobForegroundService();
#endif
        }


        public override async void OnContainerReady(IServiceProvider services)
        {
            base.OnContainerReady(services);
            var manager = services.GetRequiredService<INotificationManager>();

            if (this.channels?.Any() ?? false)
                await manager.SetChannels(this.channels);

            if (requestPermissionImmediately)
                await manager.RequestAccess();
        }
    }
}
#endif