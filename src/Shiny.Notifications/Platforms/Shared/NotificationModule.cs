#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Notifications
{
    class NotificationModule : ShinyModule
    {
        readonly Type? delegateType;
        readonly bool requestPermissionImmediately;
        readonly NotificationCategory[] notificationCategories;


        public NotificationModule(Type? delegateType,
                                  bool requestPermissionImmediately,
                                  AndroidOptions? androidConfig,
                                  UwpOptions? uwpConfig,
                                  NotificationCategory[] notificationCategories)
        {
            this.delegateType = delegateType;
            this.requestPermissionImmediately = requestPermissionImmediately;
            this.notificationCategories = notificationCategories;

            if (androidConfig != null)
            {
                AndroidOptions.DefaultChannel = androidConfig.ChannelDescription ?? AndroidOptions.DefaultChannel;
                AndroidOptions.DefaultChannelDescription = androidConfig.ChannelDescription ?? AndroidOptions.DefaultChannelDescription;
                AndroidOptions.DefaultChannelId = androidConfig.ChannelId ?? AndroidOptions.DefaultChannelId;
                AndroidOptions.DefaultNotificationImportance = androidConfig.NotificationImportance;
                AndroidOptions.DefaultLaunchActivityFlags = androidConfig.LaunchActivityFlags;
                AndroidOptions.DefaultVibrate = androidConfig.Vibrate;
                AndroidOptions.DefaultSmallIconResourceName = androidConfig.SmallIconResourceName ?? AndroidOptions.DefaultSmallIconResourceName;
            }
            if (uwpConfig != null)
                UwpOptions.DefaultUseLongDuration = uwpConfig.UseLongDuration;
        }


        public override void Register(IServiceCollection services)
        {
            if (this.delegateType != null)
                services.AddSingleton(typeof(INotificationDelegate), this.delegateType);

            services.AddSingleton<INotificationManager, NotificationManager>();
#if __ANDROID__
            services.AddSingleton<AndroidNotificationProcessor>();
            services.RegisterJob(new Jobs.JobInfo(typeof(NotificationJob))
            {
                PeriodicTime = TimeSpan.FromMinutes(2),
                Repeat = true,
                IsSystemJob = true
            });
#elif WINDOWS_UWP
            services.RegisterJob(new Jobs.JobInfo(typeof(NotificationJob))
            {
                PeriodicTime = TimeSpan.FromMinutes(15),
                Repeat = true,
                IsSystemJob = true
            });
#endif
        }


        public override async void OnContainerReady(IServiceProvider services)
        {
            base.OnContainerReady(services);
            var manager = services.GetRequiredService<INotificationManager>();

            if (requestPermissionImmediately)
                await manager.RequestAccess();

            foreach (var category in this.notificationCategories)
                manager.RegisterCategory(category);
        }
    }
}
#endif