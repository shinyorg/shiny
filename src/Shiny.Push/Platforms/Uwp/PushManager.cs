using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using Windows.Networking.PushNotifications;
using Shiny.Logging;
using Shiny.Settings;


namespace Shiny.Push
{
    public class PushManager : AbstractPushManager
    {
        readonly Subject<IPushNotification> pushSubject;
        readonly IServiceProvider serviceProvider;


        public PushManager(IServiceProvider serviceProvider, ISettings settings) : base(settings)
        {
            this.pushSubject = new Subject<IPushNotification>();
            this.serviceProvider = serviceProvider;
        }


        public override IObservable<IPushNotification> WhenReceived() => this.pushSubject;


        public override async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            //channel.ExpirationTime - persist and deal with this
            channel.PushNotificationReceived += this.OnPushNotification;
            //this.pushSubject.OnNext(push);

            return new PushAccessState(AccessState.Available, channel.Uri);
        }


        public override Task UnRegister() => Task.CompletedTask;


        void OnPushNotification(PushNotificationChannel sender, PushNotificationReceivedEventArgs e) => Log.SafeExecute(async () =>
        {
            //await this.serviceProvider.Resolve<IPushDelegate>()?.OnReceived(e.RawNotification.Content);
            //this.CurrentRegistrationToken = e.RawNotification.Ur
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;
            e.Cancel = true;
        });
    }
}
