using System;
using System.Linq;
using Android.App.Job;
using Android.Content;
using Java.Lang;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Jobs
{
    public static class ServicesExtension
    {
        public static void ConfigureJobService(this ServiceCollection services, TimeSpan serviceInterval)
            => services.RegisterPostBuildAction(sp =>
            {
                var context = sp.GetService<AndroidContext>();
                PeriodicJobInterval = serviceInterval;
                context.StopJobService();
                context.StartJobService();
            });


        public static TimeSpan PeriodicJobInterval { get; private set; } = TimeSpan.FromMinutes(10);
        public const int ANDROID_JOB_ID = 100;



        public static JobScheduler NativeScheduler(this AndroidContext context) => (JobScheduler)context.AppContext.GetSystemService(JobService.JobSchedulerService);
        public static void StopJobService(this AndroidContext context) => context.NativeScheduler().Cancel(ANDROID_JOB_ID);
        public static void StartJobService(this AndroidContext context)
        {
            var sch = context.NativeScheduler();
            if (!sch.AllPendingJobs.Any(x => x.Id == ANDROID_JOB_ID))
            {
                var job = new Android.App.Job.JobInfo.Builder(
                        ANDROID_JOB_ID,
                        new ComponentName(
                            context.AppContext,
                            Class.FromType(typeof(ShinyJobService))
                        )
                    )
                    .SetPeriodic(Convert.ToInt64(PeriodicJobInterval.TotalMilliseconds))
                    .SetPersisted(true)
                    .Build();

                sch.Schedule(job);
            }
        }
    }
}
