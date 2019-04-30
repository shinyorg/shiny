using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.IO;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Settings;
using Shiny.Testing.Jobs;
using Shiny.Testing.Net;
using Shiny.Testing.Power;
using Shiny.Testing.Settings;


namespace Shiny.Testing
{
    public class ShinyTestHost : ShinyHost
    {
        public static void Init(Startup startup = null, Action<IServiceCollection> platformBuild = null)
        {
            InitPlatform(startup, services =>
            {
                services.AddSingleton<IJobManager, TestJobManager>();
                services.AddSingleton<IConnectivity, TestConnectivity>();
                services.AddSingleton<IPowerManager, TestPowerManager>();
                services.AddSingleton<IFileSystem, FileSystemImpl>();
                services.AddSingleton<ISettings, TestSettings>();
                services.AddSingleton<IEnvironment, TestEnvironment>();
                platformBuild?.Invoke(services);
            });
        }
    }
}
