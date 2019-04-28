## Querying & Events

```csharp
// Listening for job(s) to Finish when not running on-demand
CrossJobs.Current.JobFinished += (sender, args) =>
{
    args.Job.Name // etc
    args.Success
    args.Exception
}

// Get Current Jobs (this does not include Tasks!)
var jobs = CrossJobs.Current.GetJobs();


// Get Job Logs - All Variables involved are optional filters
var logs = CrossJobs.Current.GetLogs(
    jobName,  // for a specific job
    DateTime.Yesterday, // all logs since this date/time (UTC based)
    errorsOnly // boolean to review logs that errored only
);
```

## Built-In Jobs
```csharp
JobManager.ScheduleLogTrimmingJob(TimeSpan); // defaults to 30 days if you don't specify
```