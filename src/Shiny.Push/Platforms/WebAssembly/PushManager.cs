using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Shiny.Infrastructure;

namespace Shiny.Push.Blazor;


public class PushManager : IPushManager, IShinyWebAssemblyService
{
    IJSInProcessObjectReference jsModule = null!;


    public async Task OnStart(IJSInProcessRuntime jsRuntime)
        => this.jsModule = await jsRuntime.ImportInProcess("Shiny.Push.Blazor", "push.js");


    public DateTime? CurrentRegistrationTokenDate => throw new NotImplementedException();
    public string? CurrentRegistrationToken => throw new NotImplementedException();
    
    public Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default) => throw new NotImplementedException();
    public Task UnRegister() => throw new NotImplementedException();
    public IObservable<PushNotification> WhenReceived() => throw new NotImplementedException();
}