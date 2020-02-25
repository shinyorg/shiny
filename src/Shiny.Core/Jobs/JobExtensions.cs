using System;
using Shiny.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;
using Shiny.Jobs.Infrastructure;

namespace Shiny
{
    public static class JobExtensions
    {
        /// <summary>
        /// Register a job on the job manager
        /// </summary>
        /// <param name="services"></param>
        /// <param name="jobInfo"></param>
        public static void RegisterJob(this IServiceCollection services,
                                       JobInfo jobInfo,
                                       JobForegroundRunStates? states = null,
                                       TimeSpan? foregroundInterval = null)
        {
            services.RegisterPostBuildAction(async sp =>
            {
                // what if permission fails?
                var jobs = sp.GetService<IJobManager>();
                var access = await jobs.RequestAccess();
                if (access == AccessState.Available)
                    await jobs.Schedule(jobInfo);
            });
            services.RegisterJobAppState(jobInfo, states, foregroundInterval);
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
                                       JobForegroundRunStates? states = null,
                                       TimeSpan? foregroundInterval = null)
        {
            services.RegisterPostBuildAction(async sp =>
            {
                // what if permission fails?
                var jobs = sp.GetService<IJobManager>();
                var access = await jobs.RequestAccess();
                if (access == AccessState.Available)
                {
                    await jobs.Schedule(new JobInfo(jobType, identifier)
                    {
                        RequiredInternetAccess = requiredNetwork,
                        Repeat = true
                    });
                }
            });
            services.RegisterJobAppState(jobInfo, states, foregroundInterval);
        }


        static void RegisterJobAppState(this IServiceCollection services,
                                        JobInfo jobInfo,
                                        JobForegroundRunStates? states,
                                        TimeSpan? foregroundInterval)
        {
            if (states != null || foregroundInterval != null)
                services.AddAppState(new JobAppStateDelegate(jobInfo.Identifier, states, foregroundInterval));
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
                return ShinyHost.Container.GetService<ISerializer>().Deserialize<T>(s);

            // TODO: Jobject & jarray tend to emerge causing this to fail
            return defaultValue;
        }
    }
}
