using System;
using System.Linq;
using Android.OS;
using Android.App.Job;
using NativeJob = Android.App.Job.JobInfo;
using JobBuilder = Android.App.Job.JobInfo.Builder;


namespace Shiny.Jobs
{
    static class PlatformExtensions
    {
        const string ShinyJobIdentifierKey = "ShinyJobId";


        public static JobBuilder SetShinyIdentifier(this JobBuilder builder, string jobIdentifier)
        {
            var bundle = new PersistableBundle();
            bundle.PutString(ShinyJobIdentifierKey, jobIdentifier);
            builder.SetExtras(bundle);
            return builder;
        }


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
