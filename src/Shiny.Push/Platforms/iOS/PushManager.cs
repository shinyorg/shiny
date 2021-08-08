using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Push.Infrastructure;


namespace Shiny.Push
{
    public sealed class PushManager : IPushManager, IShinyStartupTask
    {
        readonly PushContainer container;
        readonly INativeAdapter adapter;


        public PushManager(PushContainer container, INativeAdapter adapter)
        {
            this.adapter = adapter;
            this.container = container;
        }


        public DateTime? CurrentRegistrationTokenDate => this.container.CurrentRegistrationTokenDate;
        public string? CurrentRegistrationToken => this.container.CurrentRegistrationToken;


        public void Start()
        {
            // TODO: should force hooks for adapter here
            if (!this.CurrentRegistrationToken.IsEmpty())
            {
                this.adapter
                    .RequestAccess()
                    .ContinueWith(x =>
                    {
                        // check if token empty
                        //if (x.IsCompletedSuccessfully &&
                        //    (!x.Result.RegistrationToken.Equals(this.CurrentRegistrationToken) ?? false))
                        //{
                        //    this.container.SetCurrentToken(x.Result!.RegistrationToken, true);
                        //}
                    });
            }
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
