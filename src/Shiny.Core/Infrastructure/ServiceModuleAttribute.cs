using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Infrastructure
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = true)]
    public abstract class ServiceModuleAttribute : Attribute
    {
        public abstract void Register(IServiceCollection services);
    }
}
