using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;


namespace Shiny
{
    public class ShinyPushFirebaseAttribute : ServiceModuleAttribute
    {
        public ShinyPushFirebaseAttribute(Type delegateType) => this.DelegateType = delegateType;
        public Type DelegateType { get; }
        public override void Register(IServiceCollection services)
            => services.UseFirebaseMessaging(this.DelegateType);
    }
}
