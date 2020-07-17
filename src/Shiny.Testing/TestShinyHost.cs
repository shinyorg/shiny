using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Testing
{
    public class TestShinyHost : ShinyHost
    {
        public static void Init(IShinyStartup? startup = null, Action<IServiceCollection>? platformBuild = null) => InitPlatform(startup, platformBuild);
        public static void UnInit() => Destroy();
    }
}
