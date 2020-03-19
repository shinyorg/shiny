using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Notifications;


namespace Shiny.Push
{
    public class PushModule : ShinyModule
    {
        readonly Type pushManagerType;
        readonly Type delegateType;
        readonly bool requestAccessOnStart;
        readonly NotificationCategory[] categories;


        public PushModule(Type pushManagerType,
                          Type delegateType,
                          bool requestAccessOnStart,
                          NotificationCategory[] categories)
        {
            this.pushManagerType = pushManagerType;
            this.delegateType = delegateType;
            this.requestAccessOnStart = requestAccessOnStart;
            this.categories = categories;
        }


        public override void Register(IServiceCollection services)
        {
#if __IOS__
            services.TryAddSingleton<iOSNotificationDelegate>();
#endif
            services.UseNotifications<PushNotificationDelegate>(false, this.categories);
            services.AddSingleton(typeof(IPushManager), this.pushManagerType);
            if (delegateType != null)
                services.AddSingleton(typeof(IPushDelegate), this.delegateType);
        }


        public override async void OnContainerReady(IServiceProvider services)
        {
            if (this.requestAccessOnStart)
            {
                await services
                    .Resolve<IPushManager>()
                    .RequestAccess();
            }
        }
    }
}
