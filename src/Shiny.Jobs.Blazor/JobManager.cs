using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Shiny.Infrastructure;
using Shiny.Net;
using Shiny.Power;
using Shiny.Stores;

namespace Shiny.Jobs.Blazor;


public class JobManager : AbstractJobManager, IShinyWebAssemblyService
{
    readonly IJSRuntime jsRuntime;
    readonly IBattery battery;
    readonly IConnectivity connectivity;
    IJSInProcessObjectReference jsRef = null!;


    public JobManager(
        IServiceProvider services,
        ILogger<IJobManager> logger,
        IRepository<JobInfo> repository,
        IBattery battery,
        IConnectivity connectivity
    )
    : base(services, repository, logger)
    {
        this.battery = battery;
        this.connectivity = connectivity;
    }


    public async Task OnStart(IJSInProcessRuntime jsRuntime)
    {
        // TODO: foreground timer
        this.jsRef = await jsRuntime.ImportInProcess("Shiny.Jobs", "jobs.js");
    }

    public override Task<AccessState> RequestAccess() => this.jsRef.RequestAccess();
    
    protected override void CancelNative(JobInfo jobInfo) => throw new NotImplementedException();
    protected override void RegisterNative(JobInfo jobInfo) => throw new NotImplementedException();
}