using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Notifications
{
    public static class ContainerBuilderExtensions
    {
        public static bool UseNotifications(this IServiceCollection builder, Action<AndroidOptions, UwpOptions> configure = null)
        {
            var androidOpts = new AndroidOptions();
            var uwpOpts = new UwpOptions();

            configure(androidOpts, uwpOpts);

            AndroidOptions.DefaultChannel = androidOpts.Channel;
            UwpOptions.DefaultUseLongDuration = uwpOpts.UseLongDuration;
#if NETSTANDARD
            return false;
#else
            builder.AddSingleton<INotificationManager, NotificationManagerImpl>();
            return true;
#endif
        }
    }
}
