using System;
using System.Threading;
using System.Threading.Tasks;

using Samples.Infrastructure;

using Shiny;
using Shiny.Jobs;


namespace Samples.Jobs
{
    public class SampleJob : IJob, IShinyStartupTask
    {
        readonly CoreDelegateServices services;
        public SampleJob(CoreDelegateServices services) => this.services = services;



        public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            await this.services.Notifications.Send(
                this.GetType(),
                true,
                "Job Started",
                $"{jobInfo.Identifier} Started"
            );
            var seconds = jobInfo.Parameters.Get("SecondsToRun", 10);
            await Task.Delay(TimeSpan.FromSeconds(seconds), cancelToken);

            await this.services.Notifications.Send(
                this.GetType(),
                false,
                "Job Finished",
                $"{jobInfo.Identifier} Finished"
            );
        }

        public void Start()
            => this.services.Notifications.Register(this.GetType(), true, "Jobs");
    }
}
