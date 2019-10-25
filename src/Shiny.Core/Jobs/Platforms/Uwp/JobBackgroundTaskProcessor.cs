using System;
using System.Threading;
using Windows.ApplicationModel.Background;


namespace Shiny.Jobs
{
    public class JobBackgroundTaskProcessor : IBackgroundTaskProcessor
    {
        readonly IJobManager jobManager;


        public JobBackgroundTaskProcessor(IJobManager jobManager)
        {
            this.jobManager = jobManager;
        }


        public async void Process(string taskName, IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            using (var cancelSrc = new CancellationTokenSource())
            {
                taskInstance.Canceled += (sender, args) => cancelSrc.Cancel();
                // task name is the jobId now
                await this.jobManager.Run(taskName, cancelSrc.Token);
            }
            deferral.Complete();
        }
    }
}
