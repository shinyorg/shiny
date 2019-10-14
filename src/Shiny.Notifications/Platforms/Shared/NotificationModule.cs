#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Notifications
{
    class NotificationModule : ShinyModule
    {
        readonly Type delegateType;
        readonly bool requestPermissionImmediately;


        public NotificationModule(Type delegateType,
                                  bool requestPermissionImmediately,
                                  AndroidOptions androidConfig,
                                  UwpOptions uwpConfig)
        {
            this.delegateType = delegateType;
            this.requestPermissionImmediately = requestPermissionImmediately;

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
#endif

#if __ANDROID__ || WINDOWS_UWP
            services.RegisterJob(new Jobs.JobInfo
            {
                Identifier = nameof(NotificationJob),
                Type = typeof(NotificationJob),
                Repeat = true
            });
#endif
        }


        public override async void OnContainerReady(IServiceProvider services)
        {
            base.OnContainerReady(services);
            if (requestPermissionImmediately)
            {
                await services
                    .GetService<INotificationManager>()
                    .RequestAccess();
            }
        }
    }
}
#endif