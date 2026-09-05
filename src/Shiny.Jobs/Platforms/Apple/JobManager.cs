using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BackgroundTasks;
using UIKit;

namespace Shiny.Jobs;


public class JobManager(
    IServiceProvider container,
    ILogger<IJobManager> logger,
    JobRegistrar registrar
) : AbstractJobManager(container, logger, registrar), IShinyStartupTask
{
    const string EX_MSG = "Could not register background processing job. Shiny uses background processing when enabled in your info.plist.  Please follow the Shiny readme for Shiny.Core to properly register BGTaskSchedulerPermittedIdentifiers";
    bool registeredSuccessfully = false;

    public void Start()
    {
#if IOS || TVOS
        if (ObjCRuntime.Runtime.Arch == ObjCRuntime.Arch.SIMULATOR)
            return;
#endif

        try
        {
            this.RegisterBgTask(GetCategoryId(false, false));
            this.RegisterBgTask(GetCategoryId(true, false));
            this.RegisterBgTask(GetCategoryId(false, true));
            this.RegisterBgTask(GetCategoryId(true, true));
            this.registeredSuccessfully = true;

            foreach (var (_, reg) in this.GetJobs())
                this.ScheduleNative(reg);
        }
        catch (Exception ex)
        {
            this.Log.LogCritical(new Exception(EX_MSG, ex), "Background tasks are not setup properly");
        }
    }


    public override Task<AccessState> RequestAccess()
    {
        var result = AccessState.Available;
        if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            result = AccessState.NotSupported;

#if IOS || TVOS
        else if (ObjCRuntime.Runtime.Arch == ObjCRuntime.Arch.SIMULATOR)
            result = AccessState.NotSupported;
#endif

        else if (!AppleExtensions.HasBackgroundMode("processing"))
            result = AccessState.NotSetup;

        else if (!this.registeredSuccessfully)
            result = AccessState.NotSetup;

        return Task.FromResult(result);
    }


    void RegisterBgTask(string identifier)
    {
        BGTaskScheduler.Shared.Register(
            identifier,
            null,
            async task =>
            {
                // NOTE: the CancellationTokenSource is intentionally NOT disposed via `using`.
                // iOS holds a native reference to the expiration handler below for the lifetime
                // of the task; disposing the CTS while that reference is live causes the native
                // callback to dereference a freed object (SIGSEGV). We dispose only once we know
                // the task has fully completed.
                var cancelSrc = new CancellationTokenSource();

                // iOS gives a very short window after expiration to mark the task complete.
                // SetTaskCompleted must run exactly once - whether the work finished or the OS
                // expired us - otherwise the reclaimed native task becomes a dangling pointer.
                var completed = 0;
                void Complete(bool success)
                {
                    if (Interlocked.Exchange(ref completed, 1) == 0)
                    {
                        task.SetTaskCompleted(success);
                        cancelSrc.Dispose();
                    }
                }

                task.ExpirationHandler = () =>
                {
                    cancelSrc.Cancel();
                    Complete(false);
                };

                try
                {
                    var jobs = this.GetJobsByCategory(task.Identifier);
                    foreach (var job in jobs)
                        await this.RunJob(job, cancelSrc.Token).ConfigureAwait(false);

                    Complete(true);
                }
                catch (Exception ex)
                {
                    this.Log.LogError(ex, "Error running background job batch for '{Identifier}'", task.Identifier);
                    Complete(false);
                }
            }
        );
    }


    void ScheduleNative(JobRegistration reg)
    {
#if IOS || TVOS
        if (ObjCRuntime.Runtime.Arch == ObjCRuntime.Arch.SIMULATOR)
            return;
#endif

        var identifier = GetCategoryId(
            reg.DeviceCharging,
            reg.RequiredInternetAccess != InternetAccess.None
        );
        var request = new BGProcessingTaskRequest(identifier);
        request.RequiresExternalPower = reg.DeviceCharging;
        request.RequiresNetworkConnectivity = reg.RequiredInternetAccess != InternetAccess.None;

        if (!BGTaskScheduler.Shared.Submit(request, out var e))
            throw new InvalidOperationException(e.LocalizedDescription.ToString());
    }
}
