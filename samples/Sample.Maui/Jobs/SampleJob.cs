using Shiny.Jobs;
using Shiny.Notifications;

namespace Sample;


public partial class SampleJob : Job
{
    readonly INotificationManager notificationManager;

    public SampleJob(ILogger<SampleJob> logger, INotificationManager notificationManager) : base(logger)
    { 
        this.notificationManager = notificationManager;
        this.MinimumTime = TimeSpan.FromSeconds(50);
    }

    protected override async Task Run(CancellationToken cancelToken)
    {
        await this.notificationManager.Send(
            "Jobs",
            $"Job Started - {this.JobInfo!.Identifier}"
        );

        await this.notificationManager.Send(
            "Jobs",
            $"Job Finished - {this.JobInfo!.Identifier}"
        );
    }
}

//public class SampleJob : IJob
//{
//    readonly INotificationManager notificationManager;
//    public SampleJob(INotificationManager notificationManager)
//        => this.notificationManager = notificationManager;

//    // TODO: test new object binding

//    public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
//    {
//        await this.notificationManager.Send(
//            "Jobs",
//            $"Job Started - {jobInfo.Identifier}"
//        );
//        //await Task.Delay(TimeSpan.FromSeconds(seconds), cancelToken);

//        await this.notificationManager.Send(
//            "Jobs",
//            $"Job Finished - {jobInfo.Identifier}"
//        );
//    }
//}
