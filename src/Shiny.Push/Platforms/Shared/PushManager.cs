#if !NETSTANDARD
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Push.Infrastructure;


namespace Shiny.Push
{
    public sealed partial class PushManager : IPushManager, IShinyStartupTask
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
        public string[]? RegisteredTags => this.container.RegisteredTags;


        public async void Start()
        {
            this.adapter.OnTokenRefreshed = async token =>
            {
                this.container.SetCurrentToken(token, false);
                await this.container.OnTokenRefreshed(token).ConfigureAwait(false);
            };

            this.adapter.OnReceived = push => this.container.OnReceived(push);
            this.adapter.OnEntry = push => this.container.OnEntry(push);

            await this.container
                .TryAutoStart(this.adapter, this.logger)
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
#endif