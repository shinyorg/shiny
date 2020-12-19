using System;
using Shiny.Jobs;
using Samples.Models;
using Shiny;
using Shiny.Infrastructure;


namespace Samples.Jobs
{
    public class JobLoggerTask : IShinyStartupTask
    {
        readonly IJobManager jobManager;
        readonly SampleSqliteConnection conn;
        readonly ISerializer serializer;


        public JobLoggerTask(IJobManager jobManager,
                             ISerializer serializer,
                             SampleSqliteConnection conn)
        {
            this.jobManager = jobManager;
            this.serializer = serializer;
            this.conn = conn;
        }


        public void Start()
        {
            this.jobManager.JobStarted.SubscribeAsync(args => this.conn.InsertAsync(new JobLog
            {
                JobIdentifier = args.Identifier,
                JobType = args.Type.FullName,
                Started = true,
                Timestamp = DateTime.Now,
                Parameters = this.serializer.Serialize(args.Parameters)
            }));
            this.jobManager.JobFinished.SubscribeAsync(args => this.conn.InsertAsync(new JobLog
            {
                JobIdentifier = args.Job.Identifier,
                JobType = args.Job.Type.FullName,
                Error = args.Exception?.ToString(),
                Parameters = this.serializer.Serialize(args.Job.Parameters),
                Timestamp = DateTime.Now
            }));
        }
    }
}
