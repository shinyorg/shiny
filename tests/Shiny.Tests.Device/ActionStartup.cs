using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Shiny.Tests
{
    public class ActionStartup : IShinyStartup
    {
        public Action<ILoggingBuilder>? BuildLogging { get; set; }
        public Action<IServiceCollection>? BuildServices { get; set; }


        public void ConfigureLogging(ILoggingBuilder builder, IPlatform platform) => this.BuildLogging?.Invoke(builder);
        public void ConfigureServices(IServiceCollection services, IPlatform platform) => this.BuildServices?.Invoke(services);
        public IServiceProvider CreateServiceProvider(IServiceCollection services) => null;
    }
}
