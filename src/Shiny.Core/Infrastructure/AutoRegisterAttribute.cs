using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Infrastructure
{

    [AttributeUsage(AttributeTargets.Assembly)]
    public abstract class AutoRegisterAttribute : Attribute
    {
        public abstract void Register(IServiceCollection services);
    }
}
