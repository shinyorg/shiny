using System;
using System.Linq;
using System.Collections.Generic;
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
        readonly Subject<IDictionary<string, string>> pushSubject;
        readonly IServiceProvider serviceProvider;
        //PushNotificationChannel channel;


        public PushManager(IServiceProvider serviceProvider, ISettings settings) : base(settings)
        {
            this.pushSubject = new Subject<IDictionary<string, string>>();
            this.serviceProvider = serviceProvider;

            //PushNotificationChannelManager
            //    .CreatePushNotificationChannelForApplicationAsync()
            //    .AsTask()
            //    .ContinueWith(x =>
            //    {

            //    });
        }


        public override IObservable<IDictionary<string, string>> WhenReceived() => this.pushSubject;


        public override async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            //channel.ExpirationTime - persist and deal with this
            channel.PushNotificationReceived += this.OnPushNotification;

            return new PushAccessState(AccessState.Available, channel.Uri);
        }


        public override Task UnRegister() => Task.CompletedTask;


        async void OnPushNotification(PushNotificationChannel sender, PushNotificationReceivedEventArgs e) => await this.serviceProvider.SafeResolveAndExecute<IPushDelegate>(async ndelegate =>
        {
            var headers = e
                .RawNotification?
                .Headers?
                .ToDictionary(
                    x => x.Key,
                    x => x.Value
                ) ?? new Dictionary<string, string>(0);

            await ndelegate.OnReceived(headers);
            this.pushSubject.OnNext(headers);
        });
                //e.BadgeNotification
                //await this.serviceProvider.Resolve<IPushDelegate>()?.OnReceived(e.RawNotification.Content);
                //this.CurrentRegistrationToken = e.RawNotification.Ur
                //this.CurrentRegistrationTokenDate = DateTime.UtcNow;
                //e.Cancel = true;
    }
}
