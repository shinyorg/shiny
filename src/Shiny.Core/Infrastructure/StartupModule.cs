using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Shiny.Jobs;

namespace Shiny.Infrastructure
{
    class StartupModule : IShinyStartupTask
    {
        static readonly List<ShinyModule> modules = new List<ShinyModule>();
        public static void AddModule(ShinyModule module)
            => modules.Add(module);


        static readonly List<JobInfo> jobs = new List<JobInfo>();
        public static void AddJob(JobInfo jobInfo) => jobs.Add(jobInfo);


        readonly IJobManager jobManager;
        readonly IServiceProvider serviceProvider;
        readonly ILogger logger;


        public StartupModule(IJobManager jobManager,
                             IServiceProvider serviceProvider,
                             ILogger<StartupModule> logger)
        {
            this.jobManager = jobManager;
            this.logger = logger;
        }


        public async void Start()
        {
            if (jobs.Count > 0)
            {
                var access = await this.jobManager.RequestAccess();
                if (access == AccessState.Available)
                {
                    foreach (var job in jobs)
                        await this.jobManager.Register(job);
                }
                else
                {
                    this.logger.LogWarning("Permissions to run jobs insufficient: " + access);
                }
            }

            foreach (var module in modules)
                module.OnContainerReady(this.serviceProvider);

            jobs.Clear();
            modules.Clear();
        }
    }
}
