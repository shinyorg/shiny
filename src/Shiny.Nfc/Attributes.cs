using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Infrastructure;
using Shiny.Nfc;

[assembly: ShinyNfcAutoRegisterAttribute]

namespace Shiny
{
    public class ShinyNfcAutoRegisterAttribute : AutoRegisterAttribute
    {
        public override void Register(IServiceCollection services)
        {
            var implType = this.FindImplementationType(typeof(INfcDelegate), false);
            services.UseNfc(implType);
        }
    }


    public class ShinyNotificationsAttribute : ServiceModuleAttribute
    {
        public ShinyNotificationsAttribute(Type delegateType)
            => this.DelegateType = delegateType;

        public Type DelegateType { get; set; }
        public override void Register(IServiceCollection services)
            => services.UseNfc(this.DelegateType);
    }
}
