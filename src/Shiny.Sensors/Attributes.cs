using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;

[assembly: Shiny.ShinySensorsAutoRegister]

namespace Shiny
{
    public class ShinySensorsAutoRegisterAttribute : AutoRegisterAttribute
    {
        public override void Register(IServiceCollection services)
        {
            services.UseAll();
        }
    }


    public class ShinySensorsAttribute : ServiceModuleAttribute
    {
        public override void Register(IServiceCollection services)
        {
            services.UseAll();
        }
    }
}
