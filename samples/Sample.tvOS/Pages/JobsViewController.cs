using Sample.tvOS.Infrastructure;
using Sample.tvOS.Services;
using Shiny.Jobs;

namespace Sample.tvOS.Pages;


/// <summary>
/// tvOS carries the same BGTaskScheduler iOS does, so jobs work identically - including the
/// Info.plist setup. As on iOS, background tasks never fire on the simulator; run this on a real
/// Apple TV if you want to see the OS schedule the job rather than only running it by hand.
/// </summary>
public class JobsViewController() : ModuleViewController(
    "Shiny.Jobs - BGTaskScheduler. Background execution does not happen on the simulator"
)
{
    protected override void OnReady()
    {
        var log = Resolve<AppLog>();
        log.Written += (_, msg) => this.Log(msg);

        this.AddAction("Registered jobs", () =>
        {
            var jobs = Resolve<IJobManager>();
            var registrations = jobs.GetJobs();
            this.Log($"{registrations.Count} registered job(s)");
            foreach (var (type, reg) in registrations)
                this.Log($"  {type.Name}  internet={reg.RequiredInternetAccess} charging={reg.DeviceCharging}");
            return Task.CompletedTask;
        });

        this.AddAction("Run now", async () =>
        {
            var jobs = Resolve<IJobManager>();
            this.Log("running HeartbeatJob inline...");
            var result = await jobs.RunJob(typeof(HeartbeatJob));
            this.Log(result.Success
                ? $"{result.Job?.JobType.Name} completed"
                : $"{result.Job?.JobType.Name} failed: {result.Exception!.Message}"
            );
        });

        this.AddAction("Run all", async () =>
        {
            var jobs = Resolve<IJobManager>();
            var results = await jobs.RunAll();
            foreach (var result in results)
                this.Log($"{result.Job?.JobType.Name} -> {(result.Success ? "ok" : result.Exception!.Message)}");
        });

        foreach (var entry in Resolve<AppLog>().Entries)
            this.Log(entry);
    }
}
