using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.BluetoothLE.Central;
using Shiny.Infrastructure;

[assembly: Shiny.ShinyBleAutoRegister]

namespace Shiny
{
    public class ShinyBleCentralAttribute : ServiceModuleAttribute
    {
        public ShinyBleCentralAttribute(Type? delegateType = null)
            => this.DelegateType = delegateType;

        public Type? DelegateType { get; set; }
        public override void Register(IServiceCollection services)
            => services.UseBleCentral(this.DelegateType);
    }


    public class ShinyBlePeripheralAttribute : ServiceModuleAttribute
    {
        public override void Register(IServiceCollection services)
            => services.UseBlePeripherals();
    }


    public class ShinyBleAutoRegisterAttribute : AutoRegisterAttribute
    {
        public override void Register(IServiceCollection services)
        {
            var implType = this.FindImplementationType(typeof(IBleCentralDelegate), false);
            services.UseBleCentral(implType);
            services.UseBlePeripherals();
        }
    }
}
