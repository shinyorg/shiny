using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public abstract class Startup
    {
        public abstract void ConfigureServices(IServiceCollection services);
        public virtual IServiceProvider CreateServiceProvider(IServiceCollection services)
            => services.BuildServiceProvider();

        public virtual void ConfigureApp(IServiceProvider provider)
        {
            var tasks = provider.GetServices<IStartupTask>();
            foreach (var task in tasks)
                task.Start();
        }
    }
}
