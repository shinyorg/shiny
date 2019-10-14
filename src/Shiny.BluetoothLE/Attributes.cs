using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;


namespace Shiny
{
    public class ShinyBleCentralAttribute : ServiceModuleAttribute
    {
        public Type DelegateType { get; set; }
        public override void Register(IServiceCollection services)
            => services.UseBleCentral(this.DelegateType);
    }


    public class ShinyBlePeripheralAttribute : ServiceModuleAttribute
    {
        public override void Register(IServiceCollection services)
            => services.UseBlePeripherals();
    }
}
