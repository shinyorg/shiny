using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BackgroundTasks;
using UIKit;
using Microsoft.Extensions.Logging;
using Shiny.Stores;

namespace Shiny.Jobs;


public class JobManager : AbstractJobManager, IShinyStartupTask
{
    const string EX_MSG = "Could not register background processing job. Shiny uses background processing when enabled in your info.plist.  Please follow the Shiny readme for Shiny.Core to properly register BGTaskSchedulerPermittedIdentifiers";
    bool registeredSuccessfully = false;


    public JobManager(
        IServiceProvider container,
        IRepository<JobInfo> repository,
        ILogger<IJobManager> logger
    ) : base(
        container,
        repository,
        logger
    ) {}


    public void Start()
    {
        try
        {
            this.Register(this.GetIdentifier(false, false));
            this.Register(this.GetIdentifier(true, false));
            this.Register(this.GetIdentifier(false, true));
            this.Register(this.GetIdentifier(true, true));
            this.registeredSuccessfully = true;
        }
        catch (Exception ex)
        {
            this.Log.LogCritical(new Exception(EX_MSG, ex), "Background tasks are not setup properly");
        }
    }

    protected void Assert()
    {
        if (!this.registeredSuccessfully)
            throw new Exception(EX_MSG);
    }


    public static bool IsAvailable
    {
        get
        {
            var result = (
                UIDevice.CurrentDevice.CheckSystemVersion(13, 0) &&
                //Runtime.Arch != Arch.SIMULATOR &&
                AppleExtensions.HasBackgroundMode("processing")
            );
            return result;
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


    public override Task<AccessState> RequestAccess()
    {
        var result = AccessState.Available;
        if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            result = AccessState.NotSupported;

        //else if (Runtime.Arch == Arch.SIMULATOR)
        //    result = AccessState.NotSupported;

        else if (!AppleExtensions.HasBackgroundMode("processing"))
            result = AccessState.NotSetup;

        return Task.FromResult(result);
    }


    protected override void CancelNative(JobInfo jobInfo)
        => BGTaskScheduler.Shared.Cancel(jobInfo.Identifier);


    protected override void RegisterNative(JobInfo jobInfo)
    {
        this.Assert();

        var identifier = this.GetIdentifier(
            jobInfo.DeviceCharging,
            jobInfo.RequiredInternetAccess == InternetAccess.Any
        );
        var request = new BGProcessingTaskRequest(identifier);
        request.RequiresExternalPower = jobInfo.DeviceCharging;
        request.RequiresNetworkConnectivity = jobInfo.RequiredInternetAccess == InternetAccess.Any;

        if (!BGTaskScheduler.Shared.Submit(request, out var e))
            throw new ArgumentException(e.LocalizedDescription.ToString());
    }


    protected void Register(string identifier)
    {
        BGTaskScheduler.Shared.Register(
            identifier,
            null,
            async task =>
            {
                using var cancelSrc = new CancellationTokenSource();

                task.ExpirationHandler = cancelSrc.Cancel;

                var jobs = await this.GetJobs();
                List<JobInfo>? jobList = null;

                switch (task.Identifier)
                {
                    case "com.shiny.job":
                        jobList = jobs
                            .Where(x =>
                                !x.DeviceCharging &&
                                x.RequiredInternetAccess == InternetAccess.None
                            )
                            .ToList();
                        break;

                    case "com.shiny.jobpower":
                        jobList = jobs
                            .Where(x =>
                                x.DeviceCharging &&
                                x.RequiredInternetAccess == InternetAccess.None
                            )
                            .ToList();
                        break;

                    case "com.shiny.jobnet":
                        jobList = jobs
                            .Where(x =>
                                !x.DeviceCharging &&
                                (
                                    x.RequiredInternetAccess == InternetAccess.Any ||
                                    x.RequiredInternetAccess == InternetAccess.Unmetered
                                )
                            )
                            .ToList();
                        break;

                    case "com.shiny.jobpowernet":
                        jobList = jobs
                            .Where(x =>
                                (
                                    x.DeviceCharging &&
                                    (
                                        x.RequiredInternetAccess == InternetAccess.Any ||
                                        x.RequiredInternetAccess == InternetAccess.Unmetered
                                    )
                                )
                                && x.DeviceCharging
                            )
                            .ToList();
                        break;
                }
                if (jobList != null)
                {
                    foreach (var job in jobList)
                    {
                        await this.Run(job.Identifier, cancelSrc.Token);
                    }
                }
                task.SetTaskCompleted(true);
            }
        );
    }


    protected string GetIdentifier(bool extPower, bool network)
    {
        //"com.shiny.job"
        //"com.shiny.jobpower"
        //"com.shiny.jobnet"
        //"com.shiny.jobpowernet"
        var id = "com.shiny.job";
        if (extPower)
            id += "power";

        if (network)
            id += "net";

        return id;
    }
}