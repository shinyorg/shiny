using System;
using System.Threading.Tasks;
using Shiny.Jobs;
using Shiny.Jobs.Infrastructure;
using Shiny.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;


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
        /// This will run any jobs marked with RunOnForeground
        /// </summary>
        /// <param name="services"></param>
        /// <param name="interval"></param>
        public static void UseJobForegroundService(this IServiceCollection services, TimeSpan? interval = null)
        {
            JobLifecycleTask.Interval = interval ?? TimeSpan.FromSeconds(30);
            services.TryAddSingleton<JobLifecycleTask>();
        }


        /// <summary>
        /// Register a job on the job manager
        /// </summary>
        /// <param name="services"></param>
        /// <param name="jobInfo"></param>
        public static void RegisterJob(this IServiceCollection services, JobInfo jobInfo)
            => StartupModule.AddJob(jobInfo);


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
                                       params (string Key, object value)[] parameters)
            => services.RegisterJob(new JobInfo(jobType, identifier)
            {
                RequiredInternetAccess = requiredNetwork,
                RunOnForeground = runInForeground,
                Repeat = true,
                Parameters = parameters?.ToDictionary()
            });


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
