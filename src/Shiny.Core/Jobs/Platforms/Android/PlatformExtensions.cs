using System;
using System.Linq;
using Android.App.Job;
using NativeJob = Android.App.Job.JobInfo;


namespace Shiny.Jobs
{
    static class PlatformExtensions
    {
        const string ShinyJobIdentifierKey = "ShinyJobId";


        public static string GetShinyJobId(this JobParameters jobParameters)
            => jobParameters.Extras.GetString(ShinyJobIdentifierKey);


        public static JobScheduler Native(this AndroidContext context)
            => (JobScheduler)context.AppContext.GetSystemService(JobService.JobSchedulerService);


        public static NativeJob GetNativeJobByShinyId(this JobScheduler native, string shinyJobIdentifier) => native
            .AllPendingJobs
            .FirstOrDefault(x =>
                x.Extras.ContainsKey(ShinyJobIdentifierKey) &&
                x.Extras.GetString(ShinyJobIdentifierKey).Equals(shinyJobIdentifier)
            );
    }
}
