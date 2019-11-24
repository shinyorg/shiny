using System;
using Shiny.IO;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Settings;
using Windows.Storage;
using Windows.ApplicationModel.Background;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace Shiny
{
    public class UwpShinyHost : ShinyHost
    {
        const string STARTUP_KEY = "ShinyStartupTypeName";
        const string MODULE_KEY = "ShinyPlatformModuleTypeName";


        public static void Init(IShinyStartup? startup = null, IShinyModule? platformModule = null)
            => InitPlatform(startup, services =>
            {
                services.TryAddSingleton<IEnvironment, EnvironmentImpl>();
                services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
                services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
                services.TryAddSingleton<IFileSystem, FileSystemImpl>();
                services.TryAddSingleton<ISettings, SettingsImpl>();
                services.TryAddSingleton<UwpContext>();

                services.TryAddSingleton<IJobManager, JobManager>();
                services.TryAddSingleton<IBackgroundTaskProcessor, JobBackgroundTaskProcessor>();

                if (platformModule != null)
                {
                    services.RegisterModule(platformModule);
                    Dehydrate(MODULE_KEY, platformModule);
                }
                if (startup != null)
                    Dehydrate(STARTUP_KEY, startup);
            });


        public static void BackgroundRun(IBackgroundTaskInstance taskInstance)
        {
            var startup = Hydrate<IShinyStartup>(STARTUP_KEY);
            var module = Hydrate<IShinyModule>(MODULE_KEY);

            Init(startup, module);
            Resolve<UwpContext>().Bridge(taskInstance);
        }


        static void Dehydrate(string key, object obj)
        {
            if (obj == null)
                return;

            ApplicationData.Current.LocalSettings.Values[key] = obj.GetType().AssemblyQualifiedName;
        }


        static T Hydrate<T>(string key) where T : class
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            if (!settings.ContainsKey(key))
                return null;

            var typeName = settings[key].ToString();
            var type = Type.GetType(typeName);
            if (type != null)
            {
                var obj = Activator.CreateInstance(type) as T;
                return obj;
            }
            return null;
        }
    }
}
