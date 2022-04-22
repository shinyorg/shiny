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
        readonly Channel[]? channels;


        public NotificationModule(Type? delegateType,
                                  AndroidOptions? androidConfig = null,
                                  Channel[]? channels = null)
        {
            this.delegateType = delegateType;
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
#if __ANDROID__ || __IOS__
            services.AddChannelManager();
#endif
#if __ANDROID__
            services.UseGeofencing<NotificationGeofenceDelegate>();
            services.TryAddSingleton<AndroidNotificationProcessor>();
            services.TryAddSingleton<AndroidNotificationManager>();
#elif WINDOWS_UWP
            services.RegisterJob(new Jobs.JobInfo(typeof(NotificationJob), runOnForeground: true));
#endif
        }


        public override async void OnContainerReady(IServiceProvider services)
        {
            base.OnContainerReady(services);
            if (this.channels?.Any() ?? false)
            {
                var manager = services.GetRequiredService<INotificationManager>();
                foreach (var channel in this.channels)
                    await manager.AddChannel(channel).ConfigureAwait(false);
            }
        }
    }
}
#endif