﻿using System;
using Shiny.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;
using Shiny.Jobs.Infrastructure;
using Shiny.Logging;


namespace Shiny
{
    public static class JobExtensions
    {
        /// <summary>
        /// This will run any jobs marked with RunOnForeground
        /// </summary>
        /// <param name="services"></param>
        public static void UseJobForegroundService(this IServiceCollection services, TimeSpan interval)
        {
            JobAppStateDelegate.Interval = interval;
            services.AddAppState<JobAppStateDelegate>();
        }


        /// <summary>
        /// Register a job on the job manager
        /// </summary>
        /// <param name="services"></param>
        /// <param name="jobInfo"></param>
        public static void RegisterJob(this IServiceCollection services, JobInfo jobInfo)
        {
            services.RegisterPostBuildAction(async sp =>
            {
                var jobs = sp.GetService<IJobManager>();
                var access = await jobs.RequestAccess();
                if (access == AccessState.Available)
                    await jobs.Schedule(jobInfo);
                else
                    Log.Write("Jobs", "Job permission failed - " + access);
            });
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
                                       bool runInForeground = false)
            => services.RegisterJob(new JobInfo(jobType, identifier)
            {
                RequiredInternetAccess = requiredNetwork,
                RunOnForeground = runInForeground,
                Repeat = true
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
                return ShinyHost.Container.GetService<ISerializer>().Deserialize<T>(s);

            // TODO: Jobject & jarray tend to emerge causing this to fail
            return defaultValue;
        }
    }
}
