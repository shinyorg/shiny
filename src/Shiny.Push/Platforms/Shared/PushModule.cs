using System;
using Microsoft.Extensions.DependencyInjection;
#if __IOS__
using Shiny.Notifications;
#endif


namespace Shiny.Push
{
    public class PushModule : ShinyModule
    {
        readonly Type pushManagerType;
        readonly Type delegateType;
        readonly bool requestAccessOnStart;


        public PushModule(Type pushManagerType,
                          Type delegateType,
                          bool requestAccessOnStart)
        {
            this.pushManagerType = pushManagerType;
            this.delegateType = delegateType;
            this.requestAccessOnStart = requestAccessOnStart;
        }


        public override void Register(IServiceCollection services)
        {
#if __IOS__
            services.RegisterIosNotificationContext();
#endif
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
