using System;
using System.Threading;
using Windows.ApplicationModel.Background;


namespace Shiny.Jobs
{
    public class JobBackgroundTask : IBackgroundTask
    {
        public static TimeSpan PeriodicRunTime { get; set; } = TimeSpan.FromMinutes(15);


        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            using (var cancelSrc = new CancellationTokenSource())
            {
                taskInstance.Canceled += (sender, reason) => cancelSrc.Cancel();
                await ShinyHost
                    .Resolve<IJobManager>()
                    .RunAll(cancelSrc.Token);

                taskInstance.GetDeferral().Complete();
            }
        }
    }
}
