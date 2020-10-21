using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public class ApplePlatform : IPlatform
    {
        public ApplePlatform()
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

        //services.TryAddSingleton<IEnvironment, EnvironmentImpl>();
        //        services.TryAddSingleton<IConnectivity, SharedConnectivityImpl>();
        //        services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
        //        services.TryAddSingleton<IJobManager, JobManager>();
        //        services.TryAddSingleton<IFileSystem, FileSystemImpl>();
        //        services.TryAddSingleton<ISettings, SettingsImpl>();
    }
}
