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


        public async void Process(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            using (var cancelSrc = new CancellationTokenSource())
            {
                taskInstance.Canceled += (sender, args) => cancelSrc.Cancel();
                await this.jobManager.RunAll(cancelSrc.Token);
            }
            deferral.Complete();
        }
    }
}
