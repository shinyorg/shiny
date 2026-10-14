using Shiny.Jobs;

namespace Shiny.DeviceTests.Tests;


// OS-scheduled background execution is out of scope - these cover registration, the run-now API and the
// foreground timer, which run the same job code the OS would
public class JobTests(IJobManager jobs, JobRunLog log, IServiceProvider services) : DeviceTestSuite
{
    public override string Name => "Jobs";


    protected override void Define()
    {
        this.Test("GetJobs_ReturnsRegistrations", _ =>
        {
            var registered = jobs.GetJobs();
            Assert.Contains(typeof(DeviceTestJob), registered.Keys);
            Assert.Contains(typeof(FailingDeviceTestJob), registered.Keys);
            Assert.DoesNotContain(typeof(UnregisteredDeviceTestJob), registered.Keys);

            Assert.True(registered[typeof(ForegroundDeviceTestJob)].RunOnForeground);
            Assert.False(registered[typeof(DeviceTestJob)].RunOnForeground);

            var criteria = registered[typeof(CriteriaDeviceTestJob)];
            Assert.Equal(InternetAccess.Unmetered, criteria.RequiredInternetAccess);
            Assert.True(criteria.DeviceCharging);
            Assert.True(criteria.BatteryNotLow);
            Assert.False(criteria.RunOnForeground);
            return Task.CompletedTask;
        });

        this.Test("RunJob_Executes", async ctx =>
        {
            var before = log.Count<DeviceTestJob>();
            var result = await jobs.RunJob(typeof(DeviceTestJob), ctx.CancellationToken).ConfigureAwait(false);

            Assert.True(result.Success, result.Exception?.ToString());
            Assert.Equal(typeof(DeviceTestJob), result.Job?.JobType);
            Assert.Equal(before + 1, log.Count<DeviceTestJob>());
            await ctx.Events.WaitAsync<string>(DeviceTestJob.RanTopic, TimeSpan.FromSeconds(5), ctx.CancellationToken).ConfigureAwait(false);
        });

        this.Test("RunJob_Failure_IsReturnedNotThrown", async ctx =>
        {
            var result = await jobs.RunJob(typeof(FailingDeviceTestJob), ctx.CancellationToken).ConfigureAwait(false);
            Assert.False(result.Success);
            Assert.IsType<InvalidOperationException>(result.Exception);
            Assert.Equal(FailingDeviceTestJob.FailureMessage, result.Exception!.Message);
        });

        this.Test("RunJob_Unregistered_Throws", async ctx =>
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => jobs.RunJob(typeof(UnregisteredDeviceTestJob), ctx.CancellationToken));
        });

        this.Test("RunAll_RunsEveryJob", async ctx =>
        {
            var before = log.Count<DeviceTestJob>();
            var results = (await jobs.RunAll(ctx.CancellationToken, runSequentially: true).ConfigureAwait(false)).ToList();

            ctx.Log($"RunAll returned {results.Count} result(s) for {jobs.GetJobs().Count} registered job(s)");
            Assert.Equal(jobs.GetJobs().Count, results.Count);
            Assert.Contains(results, x => x.Job?.JobType == typeof(DeviceTestJob) && x.Success);
            Assert.Contains(results, x => x.Job?.JobType == typeof(FailingDeviceTestJob) && !x.Success);
            Assert.Equal(before + 1, log.Count<DeviceTestJob>());
        });

        this.Test("Job_MinimumTime_Throttles", async ctx =>
        {
            var job = services.GetRequiredService<ThrottledDeviceTestJob>();
            job.LastRunTime = null;
            var before = log.Count<ThrottledDeviceTestJob>();

            var first = await jobs.RunJob(typeof(ThrottledDeviceTestJob), ctx.CancellationToken).ConfigureAwait(false);
            var second = await jobs.RunJob(typeof(ThrottledDeviceTestJob), ctx.CancellationToken).ConfigureAwait(false);

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.Equal(before + 1, log.Count<ThrottledDeviceTestJob>());
            Assert.NotNull(job.LastRunTime);
        });

        this.Test("ForegroundJob_RunsOnTimer", async ctx =>
        {
            // runs once when the app starts and then every JobLifecycleTask.Interval (1 minute)
            await ctx.WaitUntilAsync(
                () => log.Count<ForegroundDeviceTestJob>() > 0,
                TimeSpan.FromSeconds(80),
                "the foreground timer to run ForegroundDeviceTestJob"
            ).ConfigureAwait(false);

            ctx.Log($"Foreground runs: {log.Count<ForegroundDeviceTestJob>()}");
        }, TimeSpan.FromSeconds(100));
    }
}
