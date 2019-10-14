using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;


namespace Shiny
{
    public class ShinyNotificationsAttribute : ServiceModuleAttribute
    {
        public ShinyNotificationsAttribute(Type delegateType = null, bool requestPermission = false)
        {
            this.DelegateType = delegateType;
            this.RequestPermissionOnStart = requestPermission;
        }


        public bool RequestPermissionOnStart { get; set; }
        public Type DelegateType { get; set; }


        public override void Register(IServiceCollection services)
            => services.UseNotifications(this.DelegateType, this.RequestPermissionOnStart);
    }
}
