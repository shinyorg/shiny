using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Notifications
{
    public static class ContainerBuilderExtensions
    {
        public static bool UseNotifications(this IServiceCollection builder,
                                            bool requestPermissionImmediately = false,
                                            Action<AndroidOptions> androidConfigure = null,
                                            Action<UwpOptions> uwpConfigure = null)
        {
            if (androidConfigure != null)
            {
                var androidOpts = new AndroidOptions();
                androidConfigure(androidOpts);

                AndroidOptions.DefaultChannel = androidOpts.ChannelDescription ?? AndroidOptions.DefaultChannel;
                AndroidOptions.DefaultChannelDescription = androidOpts.ChannelDescription ?? AndroidOptions.DefaultChannelDescription;
                AndroidOptions.DefaultChannelId = androidOpts.ChannelId ?? AndroidOptions.DefaultChannelId;
                AndroidOptions.DefaultNotificationImportance = androidOpts.NotificationImportance;
                AndroidOptions.DefaultLaunchActivityFlags = androidOpts.LaunchActivityFlags;
                AndroidOptions.DefaultVibrate = androidOpts.Vibrate;
                AndroidOptions.DefaultSmallIconResourceName = androidOpts.SmallIconResourceName ?? AndroidOptions.DefaultSmallIconResourceName;
            }
            if (uwpConfigure != null)
            {
                var uwpOpts = new UwpOptions();
                uwpConfigure(uwpOpts);
                UwpOptions.DefaultUseLongDuration = uwpOpts.UseLongDuration;
            }

#if NETSTANDARD
            return false;
#else
            if (requestPermissionImmediately)
            {
                builder.RegisterPostBuildAction(async sp =>
                    await sp
                        .GetService<INotificationManager>()
                        .RequestAccess()
                );
            }
            builder.AddSingleton<INotificationManager, NotificationManagerImpl>();
            return true;
#endif
        }
    }
}
