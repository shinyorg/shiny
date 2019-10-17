using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Beacons;
using Shiny.Infrastructure;


namespace Shiny
{
    public class ShinyBeaconsAttribute : ShinyServiceModuleAttribute
    {
        public ShinyBeaconsAttribute(Type delegateType = null) : base(typeof(IBeaconDelegate), false)
            => this.DelegateInstanceType = delegateType;

        public Type DelegateInstanceType { get; set; }
        public override void Register(IServiceCollection services)
            => services.UseBeacons(this.DelegateInstanceType);
    }
}
