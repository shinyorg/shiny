using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;
using Shiny.Jobs.Infrastructure;
using Shiny.Infrastructure;


namespace Shiny
{
    public static class JobExtensions
    {
        /// <summary>
        /// This will run a job from an event/delegate - the same rules as the job running normally still apply
        /// You don't need to use this if you are using foreground jobs (ie. services.UseJobForegroundService)
        /// </summary>
        /// <param name="jobManager"></param>
        /// <param name="jobIdentifier"></param>
        /// <returns></returns>
        public static Task RunJobAsTask(this IJobManager jobManager, string jobIdentifier)
        {
            var tcs = new TaskCompletionSource<object?>();
            jobManager.RunTask(jobIdentifier, async ct =>
            {
                var result = await jobManager.Run(jobIdentifier, ct);
                if (result.Success)
                    tcs.SetResult(null);
                else
                    tcs.SetResult(result.Exception);
            });
            return tcs.Task;
        }


        /// <summary>
        /// Register a job on the job manager
        /// </summary>
        /// <param name="services">The service collection to register with</param>
        /// <param name="jobInfo">The job info to register</param>
        /// <param name="clearJobQueueFirst">If set to true, before registering all new jobs during startup, an command will be issued to clear out any previous jobs - this is useful during application upgrades or if you aren't manually registering jobs</param>
        public static void RegisterJob(this IServiceCollection services, JobInfo jobInfo, bool? clearJobQueueFirst = null)
        {
            services.UseJobs(clearJobQueueFirst);
            JobsStartup.AddJob(jobInfo);
        }


        /// <summary>
        /// Registers a job on the job manager
        /// </summary>
        /// <param name="services"></param>
        /// <param name="jobType"></param>
        /// <param name="identifier"></param>
        public static void RegisterJob(this IServiceCollection services,
                                       Type jobType,
                                       string? identifier = null,
                                       InternetAccess requiredNetwork = InternetAccess.None,
                                       bool runInForeground = false,
                                       bool? clearJobQueueFirst = null,
                                       params (string Key, object value)[] parameters)
        {
            services.RegisterJob(new JobInfo(jobType, identifier)
            {
                RequiredInternetAccess = requiredNetwork,
                RunOnForeground = runInForeground,
                Repeat = true,
                Parameters = parameters?.ToDictionary()
            }, clearJobQueueFirst);
        }

        public static void SetParameter<T>(this JobInfo job, string key, T value)
            => job.Parameters[key] = value;


        public static T GetParameter<T>(this JobInfo job, string key, T defaultValue = default)
        {
            if (!job.Parameters.ContainsKey(key))
                return defaultValue;

            var value = job.Parameters[key];
            if (typeof(T).IsPrimitive)
                return (T)Convert.ChangeType(value, typeof(T));

            if (value is string s && typeof(T) != typeof(string))
                return ShinyHost.Resolve<ISerializer>().Deserialize<T>(s);

            return (T)value;
        }
    }
}
