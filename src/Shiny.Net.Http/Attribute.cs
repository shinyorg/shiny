using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;


namespace Shiny
{
    public class ShinyHttpTransfersAttribute : ServiceModuleAttribute
    {
        public ShinyHttpTransfersAttribute(Type delegateType)
            => this.DelegateType = delegateType;


        public Type DelegateType { get; }
        public override void Register(IServiceCollection services)
            => services.UseHttpTransfers(this.DelegateType);
    }
}
