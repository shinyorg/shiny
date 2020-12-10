#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace Shiny.Notifications
{
    class NotificationModule : ShinyModule
    {
        readonly Type? delegateType;
        readonly bool requestPermissionImmediately;


        public NotificationModule(Type? delegateType,
                                  bool requestPermissionImmediately,
                                  AndroidOptions? androidConfig = null,
                                  Channel[]? channels = null)
        {
            this.delegateType = delegateType;
            this.requestPermissionImmediately = requestPermissionImmediately;

            if (androidConfig != null)
            {
                AndroidOptions.DefaultLaunchActivityFlags = androidConfig.LaunchActivityFlags;
                AndroidOptions.DefaultShowWhen = androidConfig.ShowWhen;
                //AndroidOptions.DefaultVibrate = androidConfig.Vibrate;
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
            services.RegisterJob(new Jobs.JobInfo(typeof(NotificationJob))
            {
                Repeat = true,
                IsSystemJob = true
            });
#elif __IOS__
            services.TryAddSingleton<iOSNotificationDelegate>();
#elif WINDOWS_UWP
            UwpPlatform.RegisterBackground<NotificationBackgroundTaskProcessor>(builder =>
            {
                builder.SetTrigger(new Windows.ApplicationModel.Background.UserNotificationChangedTrigger(Windows.UI.Notifications.NotificationKinds.Toast));
            });
#endif
        }


        public override async void OnContainerReady(IServiceProvider services)
        {
            base.OnContainerReady(services);
            var manager = services.GetRequiredService<INotificationManager>();

            if (requestPermissionImmediately)
                await manager.RequestAccess();
        }
    }
}
#endif