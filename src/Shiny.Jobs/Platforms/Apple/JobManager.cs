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
#if IOS
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


    public override async void RunTask(string taskName, Func<CancellationToken, Task> task)
    {
        var app = UIApplication.SharedApplication;
        var taskId = 0;
        try
        {
            using var cancelSrc = new CancellationTokenSource();

            taskId = (int)app.BeginBackgroundTask(taskName, cancelSrc.Cancel);
            this.LogTask(JobState.Start, taskName);
            await task(cancelSrc.Token).ConfigureAwait(false);
            this.LogTask(JobState.Finish, taskName);
        }
        catch (Exception ex)
        {
            this.LogTask(JobState.Error, taskName, ex);
        }
        finally
        {
            app.EndBackgroundTask(taskId);
        }
    }


    protected override async Task<JobRunResult> RunAsTask(Type jobType, JobRegistration reg, CancellationToken cancellationToken)
    {
        var app = UIApplication.SharedApplication;
        var taskId = 0;
        using var cancelSrc = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            taskId = (int)app.BeginBackgroundTask(jobType.FullName, cancelSrc.Cancel);
            return await this.RunJob(jobType, reg, cancelSrc.Token).ConfigureAwait(false);
        }
        finally
        {
            app.EndBackgroundTask(taskId);
        }
    }


    public override Task<AccessState> RequestAccess()
    {
        var result = AccessState.Available;
        if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            result = AccessState.NotSupported;

#if IOS
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
                using var cancelSrc = new CancellationTokenSource();
                task.ExpirationHandler = cancelSrc.Cancel;

                var jobs = this.GetJobsByCategory(task.Identifier);
                foreach (var job in jobs)
                {
                    await this.RunJob(job, cancelSrc.Token);
                }
                task.SetTaskCompleted(true);
            }
        );
    }


    void ScheduleNative(JobRegistration reg)
    {
#if IOS
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
