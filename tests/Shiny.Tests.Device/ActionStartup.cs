using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Shiny.Tests
{
    public class ActionStartup : IShinyStartup
    {
        public Action<ILoggingBuilder>? BuildLogging { get; set; }
        public Action<IServiceCollection>? BuildServices { get; set; }

        public void UseXUnitLogging(ITestOutputHelper output)
            => this.BuildLogging = builder => builder.AddXUnit(output);

        public void ConfigureLogging(ILoggingBuilder builder, IPlatform platform) => this.BuildLogging?.Invoke(builder);
        public void ConfigureServices(IServiceCollection services, IPlatform platform) => this.BuildServices?.Invoke(services);
        public IServiceProvider CreateServiceProvider(IServiceCollection services) => null;
    }
}
