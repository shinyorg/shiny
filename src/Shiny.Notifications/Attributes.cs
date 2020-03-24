using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Infrastructure;
using Shiny.Notifications;

[assembly: ShinyNotificationsAutoRegister]

namespace Shiny
{
    public class ShinyNotificationsAutoRegisterAttribute : AutoRegisterAttribute
    {
        public override void Register(IServiceCollection services)
        {
            var implType = this.FindImplementationType(typeof(INotificationDelegate), false);
            services.UseNotifications(implType, true);
        }
    }


    public class ShinyNotificationsAttribute : ServiceModuleAttribute
    {
        public ShinyNotificationsAttribute(Type? delegateType = null, bool requestPermission = false)
        {
            this.DelegateType = delegateType;
            this.RequestPermissionOnStart = requestPermission;
        }


        public bool RequestPermissionOnStart { get; set; }
        public Type? DelegateType { get; set; }


        public override void Register(IServiceCollection services)
            => services.UseNotifications(this.DelegateType, this.RequestPermissionOnStart);
    }
}
