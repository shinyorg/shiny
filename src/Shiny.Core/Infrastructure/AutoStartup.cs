using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Infrastructure
{
    public class AutoStartup : ShinyStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.RegisterModule(new AutoRegistrationModule());
        }
    }
}
