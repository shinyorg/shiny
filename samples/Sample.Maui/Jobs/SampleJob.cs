using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny;
using Shiny.Jobs;
using Shiny.Notifications;

namespace Sample;


public class SampleJob : IJob
{
    readonly INotificationManager notificationManager;
    public SampleJob(INotificationManager notificationManager)
        => this.notificationManager = notificationManager;


    public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
    {
        await this.notificationManager.Send(
            "Jobs",
            $"Job Started - {jobInfo.Identifier}"
        );
        var seconds = jobInfo.GetParameter<int>("SecondsToRun", 10);
        await Task.Delay(TimeSpan.FromSeconds(seconds), cancelToken);

        await this.notificationManager.Send(
            "Jobs",
            $"Job Finished - {jobInfo.Identifier}"
        );
    }
}
