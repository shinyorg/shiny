using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Push;


namespace Shiny.Testing.Push
{
    public class TestPushManager : IPushManager
    {
        public AccessState ResultStatus { get; set; } = AccessState.Available;
        public DateTime? CurrentRegistrationTokenDate { get; set; }
        public string? CurrentRegistrationToken { get; set; }

        public Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default) => Task.FromResult(new PushAccessState(this.ResultStatus, this.CurrentRegistrationToken));
        public Task UnRegister() => Task.CompletedTask;
    }
}
