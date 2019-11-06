using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Beacons;
using Shiny.Infrastructure;

[assembly: Shiny.ShinyBeaconsAutoRegister]

namespace Shiny
{
    public class ShinyBeaconsAttribute : ServiceModuleAttribute
    {
        public ShinyBeaconsAttribute(Type delegateType = null)
            => this.DelegateType = delegateType;

        public Type DelegateType { get; }
        public override void Register(IServiceCollection services)
            => services.UseBeacons(this.DelegateType);
    }


    public class ShinyBeaconsAutoRegisterAttribute : AutoRegisterAttribute
    {
        public override void Register(IServiceCollection services)
        {
            var implType = this.FindImplementationType(typeof(IBeaconDelegate), false);
            services.UseBeacons(implType);
        }
    }
}
