using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Push.Infrastructure;


namespace Shiny.Push
{
    public sealed class PushManager : IPushManager, IShinyStartupTask
    {
        readonly PushContainer container;
        readonly INativeAdapter adapter;
        readonly ILogger logger;


        public PushManager(PushContainer container,
                           INativeAdapter adapter,
                           ILogger<PushManager> logger)
        {
            this.adapter = adapter;
            this.container = container;
            this.logger = logger;
        }


        public DateTime? CurrentRegistrationTokenDate => this.container.CurrentRegistrationTokenDate;
        public string? CurrentRegistrationToken => this.container.CurrentRegistrationToken;


        public async void Start()
        {
            this.adapter.OnReceived = push => this.container.OnReceived(push);
            this.adapter.OnEntry = push => this.container.OnEntry(push);
            await this.adapter
                .AutoStartIfApplicable(this.container, this.logger)
                .ConfigureAwait(false);
        }


        public IObservable<PushNotification> WhenReceived() => this.container.WhenReceived();


        public async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var result = await this.adapter.RequestAccess().ConfigureAwait(false);
            this.container.SetCurrentToken(result.RegistrationToken!, false);
            return new PushAccessState(result.Status, this.CurrentRegistrationToken);
        }


        public async Task UnRegister()
        {
            await this.adapter.UnRegister().ConfigureAwait(false);
            this.container.ClearRegistration();
        }
    }
}
