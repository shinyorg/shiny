using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Shiny.Jobs;

namespace Shiny.DeviceTests.Tests;


// survives EventHub.Clear between tests - the foreground timer runs jobs whenever it likes
public class JobRunLog
{
    readonly ConcurrentDictionary<Type, int> counts = new();

    public void Record(Type jobType) => this.counts.AddOrUpdate(jobType, 1, (_, c) => c + 1);
    public int Count<TJob>() => this.counts.TryGetValue(typeof(TJob), out var c) ? c : 0;
}


public class DeviceTestJob(JobRunLog log, EventHub events) : IJob
{
    public const string RanTopic = "jobs.ran";

    public Task Run(CancellationToken cancelToken)
    {
        log.Record(this.GetType());
        events.Publish(RanTopic, this.GetType().Name);
        return Task.CompletedTask;
    }
}


public class FailingDeviceTestJob : IJob
{
    public const string FailureMessage = "Device test job failure";

    public Task Run(CancellationToken cancelToken) => throw new InvalidOperationException(FailureMessage);
}


// Shiny's Job base skips a run when MinimumTime has not elapsed since LastRunTime
public class ThrottledDeviceTestJob : Job
{
    readonly JobRunLog log;

    public ThrottledDeviceTestJob(ILogger<ThrottledDeviceTestJob> logger, JobRunLog log) : base(logger)
    {
        this.log = log;
        this.MinimumTime = TimeSpan.FromHours(1);
    }

    protected override Task RunJob(CancellationToken cancelToken)
    {
        this.log.Record(this.GetType());
        return Task.CompletedTask;
    }
}


// registered WithForeground() - the foreground timer runs it at launch and then every minute
public class ForegroundDeviceTestJob(JobRunLog log) : IJob
{
    public Task Run(CancellationToken cancelToken)
    {
        log.Record(this.GetType());
        return Task.CompletedTask;
    }
}


// only here so GetJobs() has criteria to report - never run by the foreground timer
public class CriteriaDeviceTestJob(JobRunLog log) : IJob
{
    public Task Run(CancellationToken cancelToken)
    {
        log.Record(this.GetType());
        return Task.CompletedTask;
    }
}


// deliberately never registered
public class UnregisteredDeviceTestJob : IJob
{
    public Task Run(CancellationToken cancelToken) => Task.CompletedTask;
}
