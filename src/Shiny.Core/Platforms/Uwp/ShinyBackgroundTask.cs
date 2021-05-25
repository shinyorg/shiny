using System;
using Shiny.Jobs;
using Windows.ApplicationModel.Background;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public abstract class ShinyBackgroundTask<TStartup> : IBackgroundTask where TStartup : class, IShinyStartup, new()
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            if (!ShinyHost.IsInitialized)
                ShinyHost.Init(new UwpPlatform(null), new TStartup());

            if (taskInstance.Task.Name.StartsWith("JOB-"))
            {
                ShinyHost
                    .ServiceProvider
                    .Resolve<JobManager>(true)!
                    .Process(taskInstance);
            }
            else
            {
                var targetType = Type.GetType(taskInstance.Task.Name);
                var processor = ActivatorUtilities.GetServiceOrCreateInstance(ShinyHost.ServiceProvider, targetType) as IBackgroundTaskProcessor;
                processor?.Process(taskInstance);
            }
        }
    }
}
