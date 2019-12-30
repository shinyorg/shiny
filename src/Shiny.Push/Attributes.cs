using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Infrastructure;
using Shiny.Push;

[assembly: ShinyPushAutoRegister]


namespace Shiny
{
    public class ShinyPushAutoRegisterAttribute : AutoRegisterAttribute
    {
        public override void Register(IServiceCollection services)
        {
            var implType = this.FindImplementationType(typeof(IPushDelegate), true);
            services.UsePush(implType);
        }
    }


    public class ShinyPushAttribute : ServiceModuleAttribute
    {
        public ShinyPushAttribute(Type delegateType) => this.DelegateType = delegateType;
        public Type DelegateType { get; }
        public override void Register(IServiceCollection services) => services.UsePush(this.DelegateType);
    }
}
