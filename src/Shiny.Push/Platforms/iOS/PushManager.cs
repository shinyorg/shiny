using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using UserNotifications;
using Shiny.Notifications;
using Shiny.Infrastructure;


namespace Shiny.Push
{
    public sealed class PushManager : AbstractPushManager, IShinyStartupTask
    {
        readonly ApnManager apnManager;
        readonly Subject<PushNotification> payloadSubj;

        public PushManager(ShinyCoreServices services, ApnManager apnManager) : base(services)
        {
            this.payloadSubj = new Subject<PushNotification>();
            this.apnManager = apnManager;
        }


        public void Start()
        {
            this.Services.Lifecycle.RegisterToReceiveRemoteNotifications(async userInfo =>
            {
                var dict = userInfo.FromNsDictionary();
                var pr = new PushNotification(dict, null);
                await this.Services
                    .Services
                    .SafeResolveAndExecute<IPushDelegate>(x => x.OnReceived(pr))
                    .ConfigureAwait(false);
                this.payloadSubj.OnNext(pr);
            });

            this.Services.Lifecycle.RegisterForNotificationReceived(async response =>
            {
                if (response.Notification?.Request?.Trigger is UNPushNotificationTrigger)
                {
                    var shiny = response.FromNative();
                    var pr = new PushNotificationResponse(
                        shiny.Notification,
                        shiny.ActionIdentifier,
                        shiny.Text
                    );

                    await this.Services
                        .Services
                        .RunDelegates<IPushDelegate>(x => x.OnEntry(pr))
                        .ConfigureAwait(false);
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
                            this.CurrentRegistrationToken = x.Result!;
                            this.CurrentRegistrationTokenDate = DateTime.UtcNow;
                        }
                    });
            }
        }


        public override IObservable<PushNotification> WhenReceived() => this.payloadSubj;


        public override async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var result = await this.apnManager.RequestPermission().ConfigureAwait(false);
            if (!result)
                return PushAccessState.Denied;

            var token = await this.apnManager
                .RequestToken(cancelToken)
                .ConfigureAwait(false);

            this.CurrentRegistrationToken = token;
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;
            return new PushAccessState(AccessState.Available, this.CurrentRegistrationToken);
        }


        public override async Task UnRegister()
        {
            await this.apnManager.UnRegister().ConfigureAwait(false);
            this.ClearRegistration();
        }
    }
}
