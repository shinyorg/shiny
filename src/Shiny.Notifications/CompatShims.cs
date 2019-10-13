using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;

[assembly: Shiny.Notifications.NotificationServiceModule]


namespace Shiny.Notifications
{
    public class NotificationServiceModuleAttribute : ServiceModuleAttribute
    {
        public override void Register(IServiceCollection services)
        {
            services.UseNotifications();
        }
    }


    public static class CrossNotifications
    {
        public static INotificationManager Current => ShinyHost.Resolve<INotificationManager>();


        //public static void RegisterDelegate<>
    }
}
