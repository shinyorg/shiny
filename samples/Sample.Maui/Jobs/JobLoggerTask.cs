using System;
using Shiny.Jobs;
using Shiny;


namespace Sample
{
    public class JobLoggerTask : IShinyStartupTask
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
            this.jobManager.JobStarted.SubscribeAsync(job => this.conn.InsertAsync(new ShinyEvent
            {
                Text = $"{job.Identifier} Started",
                Detail = "",
                Timestamp = DateTime.Now,
            }));
            this.jobManager.JobFinished.SubscribeAsync(result => this.conn.InsertAsync(new ShinyEvent
            {
                Text = result.Job?.Identifier + " " + (result.Success ? "Completed" : "Failed"),
                Detail = result.Exception?.ToString() ?? "",
                Timestamp = DateTime.Now
            }));
        }
    }
}
