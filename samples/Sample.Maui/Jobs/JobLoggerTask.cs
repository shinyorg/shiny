using Shiny.Jobs;

namespace Sample.Jobs;


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
        this.jobManager.JobStarted.SubscribeAsync(job => this.conn.Log(        
            $"[JOB] {job.Identifier} Started",
            ""
        ));
        this.jobManager.JobFinished.SubscribeAsync(result => this.conn.Log(
            $"[JOB]: {result.Job?.Identifier} " + (result.Success ? "Completed" : "Failed"),
            result.Exception?.ToString() ?? ""
        ));
    }
}
