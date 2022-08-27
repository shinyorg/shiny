//using Microsoft.JSInterop;

//namespace Shiny.Push.Web;


//public class PushManager : IPushManager, IAsyncDisposable
//{
//    readonly IJSRuntime jsRuntime;
//    IJSObjectReference? jsRef;


//    public PushManager(IJSRuntime jsRuntime)
//    {
//        this.jsRuntime = jsRuntime;
//    }


//    public DateTime? CurrentRegistrationTokenDate => throw new NotImplementedException();
//    public string CurrentRegistrationToken => throw new NotImplementedException();

//    async ValueTask<IJSObjectReference> GetModule()
//    {
//        this.jsRef ??= await this.jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Shiny.Push.Web/push.js");
//        return this.jsRef;
//    }


//    public async ValueTask DisposeAsync()
//    {
//        if (this.jsRef != null)
//            await this.jsRef.DisposeAsync();
//    }

//    public IObservable<PushNotification> WhenReceived()
//    {
//        throw new NotImplementedException();
//    }

//    public Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
//    {
//        throw new NotImplementedException();
//    }

//    public Task UnRegister()
//    {
//        throw new NotImplementedException();
//    }
//}
