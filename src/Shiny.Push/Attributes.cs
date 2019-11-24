using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Infrastructure;
using Shiny.Push;

[assembly: ShinyPushNotificationsAutoRegister]

namespace Shiny
{
    public class ShinyPushNotificationsAutoRegisterAttribute : AutoRegisterAttribute
    {
        public override void Register(IServiceCollection services)
        {
            var implType = this.FindImplementationType(typeof(IPushNotificationDelegate), true);
            services.UsePushNotifications(implType);
        }
    }


    public class ShinyPushNotificationsAttribute : ServiceModuleAttribute
    {
        public ShinyPushNotificationsAttribute(Type delegateType)
        {
            this.DelegateType = delegateType;
        }


        public Type DelegateType { get; set; }


        public override void Register(IServiceCollection services)
            => services.UsePushNotifications(this.DelegateType);
    }
}
