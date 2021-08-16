#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Push.Infrastructure;


namespace Shiny.Push
{
    public class PushModule : ShinyModule
    {
        readonly Type pushManagerType;
        readonly Type delegateType;


        public PushModule(Type pushManagerType, Type delegateType)
        {
            this.pushManagerType = pushManagerType;
            this.delegateType = delegateType;
        }


        public override void Register(IServiceCollection services)
        {
            services.AddSingleton(typeof(IPushDelegate), this.delegateType);
            services.TryAddSingleton(typeof(IPushManager), this.pushManagerType);
            services.TryAddSingleton<PushContainer>();
            services.TryAddSingleton<INativeAdapter, NativeAdapter>();
            services.UseNotifications();
//#if __ANDROID__
//            services.TryAddSingleton<AndroidPushNotificationManager>();
//            services.TryAddSingleton<AndroidPushProcessor>();
//#endif
        }
    }
}
#endif