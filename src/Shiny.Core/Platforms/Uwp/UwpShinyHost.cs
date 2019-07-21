using System;
using Shiny.IO;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Settings;
using Microsoft.Extensions.DependencyInjection;
using Windows.Storage;
using Windows.ApplicationModel.Background;


namespace Shiny
{
    public class UwpShinyHost : ShinyHost
    {
        const string STARTUP_KEY = "ShinyStartupTypeName";
        const string MODULE_KEY = "ShinyPlatformModuleTypeName";


        public static void Init(IShinyStartup startup = null, IShinyModule platformModule = null)
            => InitPlatform(startup, services =>
            {
                services.AddSingleton<IEnvironment, EnvironmentImpl>();
                services.AddSingleton<IConnectivity, ConnectivityImpl>();
                services.AddSingleton<IPowerManager, PowerManagerImpl>();
                services.AddSingleton<IFileSystem, FileSystemImpl>();
                services.AddSingleton<ISettings, SettingsImpl>();
                services.AddSingleton<UwpContext>();

                services.AddSingleton<IJobManager, JobManager>();
                services.AddSingleton<IBackgroundTaskProcessor, JobBackgroundTaskProcessor>();

                if (platformModule != null)
                    services.RegisterModule(platformModule);

                Dehydrate(STARTUP_KEY, startup);
                Dehydrate(MODULE_KEY, platformModule);
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
