using System;
using System.Threading;
using UIKit;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Infrastructure;
using Shiny.IO;
using Shiny.Settings;
using Shiny.Logging;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public class IosShinyHost : ShinyHost
    {
        public static void Init(Startup startup = null, Action<IServiceCollection> platformBuild = null)
            => InitPlatform(startup, services =>
            {
                services.AddSingleton<IEnvironment, EnvironmentImpl>();
                services.AddSingleton<IConnectivity, ConnectivityImpl>();
                services.AddSingleton<IPowerManager, PowerManagerImpl>();
                services.AddSingleton<IJobManager, JobManagerImpl>();
                services.AddSingleton<IRepository, FileSystemRepositoryImpl>();
                services.AddSingleton<IFileSystem, FileSystemImpl>();
                services.AddSingleton<ISerializer, JsonNetSerializer>();
                services.AddSingleton<ISettings, SettingsImpl>();
                platformBuild?.Invoke(services);
            });


        public static async void OnBackgroundFetch(Action<UIBackgroundFetchResult> completionHandler)
        {
            var result = UIBackgroundFetchResult.NoData;
            var app = UIApplication.SharedApplication;
            var taskId = 0;

            try
            {
                using (var cancelSrc = new CancellationTokenSource())
                {
                    taskId = (int)app.BeginBackgroundTask("RunAll", cancelSrc.Cancel);
                    await Container
                        .GetService<IJobManager>()
                        .RunAll(cancelSrc.Token)
                        .ConfigureAwait(false);

                    result = UIBackgroundFetchResult.NewData;
                }
            }
            catch (Exception ex)
            {
                result = UIBackgroundFetchResult.Failed;
                Log.Write(ex);
            }
            finally
            {
                completionHandler(result);
                app.EndBackgroundTask(taskId);
            }
        }
    }
}
