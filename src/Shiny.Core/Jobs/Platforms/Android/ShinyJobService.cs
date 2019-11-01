#if !ANDROIDX
using System;
using System.Threading;
using Android.App;
using Android.App.Job;


namespace Shiny.Jobs
{
    [Service(
        Name = "com.shiny.ShinyJobService",
        Permission = "android.permission.BIND_JOB_SERVICE",
        Exported = true
    )]
    public class ShinyJobService : JobService
    {
        CancellationTokenSource cancelSrc;


        public override bool OnStartJob(JobParameters @params)
        {
            var jobIdentifier = @params.GetShinyJobId();
            this.cancelSrc = new CancellationTokenSource();
            ShinyHost
                .Resolve<IJobManager>()
                .Run(jobIdentifier, this.cancelSrc.Token)
                .ContinueWith(x => this.JobFinished(@params, false));
            return true;
        }


        public override bool OnStopJob(JobParameters @params)
        {
            this.cancelSrc?.Cancel();
            return true;
        }
    }
}
#endif