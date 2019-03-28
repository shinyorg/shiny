using System;
using Shiny.Jobs;
using Samples.Models;
using Shiny;


namespace Samples.ShinySetup
{
    public class JobLoggerTask : IStartupTask
    {
        readonly IJobManager jobManager;
        readonly SampleSqliteConnection conn;


        public JobLoggerTask(IJobManager jobManager, SampleSqliteConnection conn)
        {
            this.jobManager = jobManager;
            this.conn = conn;
        }


        public void Start()
        {
            this.jobManager.JobStarted += async (sender, args) =>
            {
                await this.conn.InsertAsync(new JobLog
                {
                    JobName = args.Identifier,
                    JobType = args.Type.FullName,
                    Started = true,
                    Timestamp = DateTime.Now
                });
            };
            this.jobManager.JobFinished += async (sender, args) =>
            {
                await this.conn.InsertAsync(new JobLog
                {
                    JobName = args.Job.Identifier,
                    JobType = args.Job.Type.FullName,
                    Error = args.Exception?.ToString(),
                    Timestamp = DateTime.Now
                });
            };
        }
    }
}
