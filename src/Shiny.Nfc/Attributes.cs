using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Infrastructure;
using Shiny.Nfc;

[assembly: ShinyNfcAutoRegisterAttribute]

namespace Shiny
{
    public class ShinyNfcAutoRegisterAttribute : AutoRegisterAttribute
    {
        public override void Register(IServiceCollection services)
        {
            services.UseNfc();
        }
    }


    public class ShinyNfcAttribute : ServiceModuleAttribute
    {
        public override void Register(IServiceCollection services)
            => services.UseNfc();
    }
}
