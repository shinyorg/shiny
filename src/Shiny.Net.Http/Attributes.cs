using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;
using Shiny.Net.Http;


namespace Shiny
{
    public class ShinyHttpTransfersAutoRegisterAttribute : AutoRegisterAttribute
    {
        public override void Register(IServiceCollection services)
        {
            var implType = this.FindImplementationType(typeof(IHttpTransferDelegate), true);
            services.UseHttpTransfers(implType);
        }
    }


    public class ShinyHttpTransfersAttribute : ServiceModuleAttribute
    {
        public ShinyHttpTransfersAttribute(Type delegateType)
            => this.DelegateType = delegateType;


        public Type DelegateType { get; }
        public override void Register(IServiceCollection services)
            => services.UseHttpTransfers(this.DelegateType);
    }
}
