using System;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny
{
    public class TizenPlatform : IPlatform
    {
        //services.TryAddSingleton<IEnvironment, EnvironmentImpl>();
        //        services.TryAddSingleton<IConnectivity, SharedConnectivityImpl>();
        //        services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
        //        //services.AddSingleton<IJobManager, JobManager>();
        //        services.TryAddSingleton<IFileSystem, FileSystemImpl>();
        //        services.AddSingleton<ISettings, SettingsImpl>();
        //        platformBuild?.Invoke(services);
        public TizenPlatform()
        {
        }

        public void Register(IServiceCollection services)
        {
            throw new NotImplementedException();
        }

        public IObservable<PlatformState> WhenStateChanged()
        {
            throw new NotImplementedException();
        }
    }
}
