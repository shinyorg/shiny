using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Shiny.Infrastructure;
using Shiny.Net;
using Shiny.Power;
using Shiny.Stores;

namespace Shiny.Jobs.Blazor;


public class JobManager : IJobManager, IShinyWebAssemblyService
{
    readonly IJSRuntime jsRuntime;
    readonly IRepository<JobInfo> repository;
    readonly IBattery battery;
    readonly IConnectivity connectivity;
    IJSInProcessObjectReference jsRef = null!;


    public JobManager(
        IJSRuntime jsRuntime,
        IRepository<JobInfo> repository,
        IBattery battery,
        IConnectivity connectivity
    )
    {
        this.jsRuntime = jsRuntime;
        this.repository = repository;
        this.battery = battery;
        this.connectivity = connectivity;
    }


    public async Task OnStart()
    {
        // TODO: foreground timer
        this.jsRef = await this.jsRuntime.ImportInProcess("Shiny.Jobs", "jobs.js");
    }



    public bool IsRunning => throw new NotImplementedException();
    public IObservable<JobInfo> JobStarted => throw new NotImplementedException();
    public IObservable<JobRunResult> JobFinished => throw new NotImplementedException();


    public async Task Cancel(string jobName)
    {
        await this.repository.Remove(jobName);
        var jobs = await this.repository.GetList();
        if (!jobs.Any())
            this.jsRef.InvokeVoid("unregister");
    }


    public async Task CancelAll()
    {
        await this.repository.Clear();
        this.jsRef.InvokeVoid("unregister");
    }


    public Task<JobInfo?> GetJob(string jobIdentifier) => throw new NotImplementedException();
    public Task<IList<JobInfo>> GetJobs() => throw new NotImplementedException();
    public Task Register(JobInfo jobInfo) => throw new NotImplementedException();
    public Task<AccessState> RequestAccess() => throw new NotImplementedException();
    public Task<JobRunResult> Run(string jobIdentifier, CancellationToken cancelToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<JobRunResult>> RunAll(CancellationToken cancelToken = default, bool runSequentially = false) => throw new NotImplementedException();
    public void RunTask(string taskName, Func<CancellationToken, Task> task) => throw new NotImplementedException();
    
}