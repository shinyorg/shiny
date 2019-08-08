using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseNotifications<TNotificationDelegate>(this IServiceCollection builder,
                                                                   bool requestPermissionImmediately = false,
                                                                   Action<AndroidOptions> androidConfigure = null,
                                                                   Action<UwpOptions> uwpConfigure = null)
                where TNotificationDelegate : class, INotificationDelegate
        {
            if (builder.UseNotifications(requestPermissionImmediately, androidConfigure, uwpConfigure))
            {
                builder.AddSingleton<INotificationDelegate, TNotificationDelegate>();
                return true;
            }
            return false;
        }


        public static bool UseNotifications(this IServiceCollection builder,
                                            bool requestPermissionImmediately = false,
                                            Action<AndroidOptions> androidConfigure = null,
                                            Action<UwpOptions> uwpConfigure = null)
        {
#if NETSTANDARD
            return false;
#else
builder.AddSingleton<NotificationProcessor>();

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

#if __ANDROID__ || WINDOWS_UWP
            builder.RegisterJob(new Jobs.JobInfo
            {
                Identifier = nameof(NotificationJob),
                Type = typeof(NotificationJob),
                Repeat = true
            });
#endif

            if (requestPermissionImmediately)
            {
                builder.RegisterPostBuildAction(async sp =>
                    await sp
                        .GetService<INotificationManager>()
                        .RequestAccess()
                );
            }
            builder.AddSingleton<INotificationManager, NotificationManager>();
            return true;
#endif
        }
    }
}
