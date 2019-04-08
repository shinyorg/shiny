using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Notifications
{
    public static class ContainerBuilderExtensions
    {
        public static bool UseNotifications(this IServiceCollection builder, Action<AndroidOptions, UwpOptions> configure = null)
        {
            if (configure != null)
            {
                var androidOpts = new AndroidOptions();
                var uwpOpts = new UwpOptions();
                configure(androidOpts, uwpOpts);

                AndroidOptions.DefaultChannel = androidOpts.ChannelDescription ?? AndroidOptions.DefaultChannel;
                AndroidOptions.DefaultChannelDescription = androidOpts.ChannelDescription ?? AndroidOptions.DefaultChannelDescription;
                AndroidOptions.DefaultChannelId = androidOpts.ChannelId ?? AndroidOptions.DefaultChannelId;
                AndroidOptions.DefaultNotificationImportance = androidOpts.NotificationImportance;
                AndroidOptions.DefaultLaunchActivityFlags = androidOpts.LaunchActivityFlags;
                AndroidOptions.DefaultVibrate = androidOpts.Vibrate;
                AndroidOptions.DefaultSmallIconResourceName = androidOpts.SmallIconResourceName ?? AndroidOptions.DefaultSmallIconResourceName;

                UwpOptions.DefaultUseLongDuration = uwpOpts.UseLongDuration;
            }

#if NETSTANDARD
            return false;
#else
            builder.AddSingleton<INotificationManager, NotificationManagerImpl>();
            return true;
#endif
        }
    }
}
