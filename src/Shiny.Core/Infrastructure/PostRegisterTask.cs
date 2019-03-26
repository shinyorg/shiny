using System;
using System.Collections.Generic;
using Shiny.Jobs;
using Shiny.Logging;


namespace Shiny.Infrastructure
{
    class PostRegisterTask : IStartupTask
    {
        public static List<JobInfo> Jobs { get; } = new List<JobInfo>();
        readonly IJobManager jobManager;

        public PostRegisterTask(IJobManager jobManager)
        {
            this.jobManager = jobManager;
        }


        public async void Start()
        {
            foreach (var job in Jobs)
            {
                try
                {
                    await this.jobManager.Schedule(job);
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                }
            }
        }
    }
}
