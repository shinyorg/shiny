using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.BluetoothLE;
using Shiny.Infrastructure;

[assembly: Shiny.ShinyBleAutoRegister]


namespace Shiny
{
    public class ShinyBleAttribute : ServiceModuleAttribute
    {
        public ShinyBleAttribute(Type? delegateType = null)
            => this.DelegateType = delegateType;

        public Type? DelegateType { get; set; }
        public override void Register(IServiceCollection services)
            => services.UseBleClient(this.DelegateType);
    }


    public class ShinyBleAutoRegisterAttribute : AutoRegisterAttribute
    {
        public override void Register(IServiceCollection services)
        {
            var implType = this.FindImplementationType(typeof(IBleDelegate), false);
            services.UseBleClient(implType);
            services.UseBleClient();
        }
    }
}
