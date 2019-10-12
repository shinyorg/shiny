using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;
using Shiny.Notifications;

[assembly: NotificationAutoRegisterAttribute]

namespace Shiny.Notifications
{
    public class NotificationAutoRegisterAttribute : AutoRegisterAttribute
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
