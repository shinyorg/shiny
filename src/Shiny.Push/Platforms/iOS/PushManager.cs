using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using UserNotifications;
using Shiny.Notifications;


namespace Shiny.Push
{
    public sealed class PushManager : IPushManager, IShinyStartupTask
    {
        readonly ApnManager apnManager;
        readonly AppleLifecycle lifecycle;
        readonly PushContainer container;
        readonly Subject<PushNotification> payloadSubj;


        public PushManager(PushContainer container,
                           AppleLifecycle lifecycle,
                           ApnManager apnManager)
        {
            this.payloadSubj = new Subject<PushNotification>();
            this.apnManager = apnManager;
            this.lifecycle = lifecycle;
            this.container = container;
        }


        public DateTime? CurrentRegistrationTokenDate => this.container.CurrentRegistrationTokenDate;
        public string? CurrentRegistrationToken => this.container.CurrentRegistrationToken;


        public void Start()
        {
            this.lifecycle.RegisterToReceiveRemoteNotifications(async userInfo =>
            {
                var dict = userInfo.FromNsDictionary();
                var pr = new PushNotification(dict, null);
                await this.container.OnReceived(pr).ConfigureAwait(false);
                this.payloadSubj.OnNext(pr);
            });

            this.lifecycle.RegisterForNotificationReceived(async response =>
            {
                if (response.Notification?.Request?.Trigger is UNPushNotificationTrigger)
                {
                    var shiny = response.FromNative();
                    var pr = new PushNotificationResponse(
                        shiny.Notification,
                        shiny.ActionIdentifier,
                        shiny.Text
                    );
                    await this.container.OnEntry(pr).ConfigureAwait(false);
                }
            });

            if (!this.CurrentRegistrationToken.IsEmpty())
            {
                this.apnManager
                    .RequestToken()
                    .ContinueWith(x =>
                    {
                        if (x.IsCompletedSuccessfully &&
                            (!x.Result?.Equals(this.CurrentRegistrationToken) ?? false))
                        {
                            this.container.SetCurrentToken(x.Result!);
                        }
                    });
            }
        }


        public IObservable<PushNotification> WhenReceived() => this.payloadSubj;


        public async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var result = await this.apnManager
                .RequestPermission()
                .ConfigureAwait(false);

            if (!result)
                return PushAccessState.Denied;

            var token = await this.apnManager
                .RequestToken(cancelToken)
                .ConfigureAwait(false);

            if (token.IsEmpty())
                return PushAccessState.Denied;

            this.container.SetCurrentToken(token!);
            return new PushAccessState(AccessState.Available, this.CurrentRegistrationToken);
        }


        public async Task UnRegister()
        {
            await this.apnManager.UnRegister().ConfigureAwait(false);
            this.container.ClearRegistration();
        }
    }
}
