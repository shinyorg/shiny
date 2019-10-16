using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;


namespace Shiny
{
    public class ShinyBeaconsAttribute : ServiceModuleAttribute
    {
        public ShinyBeaconsAttribute(Type delegateType = null)
            => this.DelegateType = delegateType;

        public Type DelegateType { get; set; }
        public override void Register(IServiceCollection services)
            => services.UseBeacons(this.DelegateType);
    }
}
