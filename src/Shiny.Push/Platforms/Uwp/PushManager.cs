using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using Windows.Networking.PushNotifications;
using Shiny.Logging;
using Shiny.Settings;
using System.Linq;
using System.IO.IsolatedStorage;

namespace Shiny.Push
{
    public class PushManager : AbstractPushManager
    {
        readonly Subject<IDictionary<string, string>> pushSubject;
        readonly IServiceProvider serviceProvider;


        public PushManager(IServiceProvider serviceProvider, ISettings settings) : base(settings)
        {
            this.pushSubject = new Subject<IDictionary<string, string>>();
            this.serviceProvider = serviceProvider;
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


        async void OnPushNotification(PushNotificationChannel sender, PushNotificationReceivedEventArgs e) => await this.serviceProvider.SafeResolveAndExecute<IPushDelegate>(async sdelegate =>
        {
            //using (var s = new IsolatedStorageFileStream("", System.IO.FileMode.Open))
            //{

            //}
            await sdelegate.OnReceived(e
                .RawNotification
                .Headers
                .ToDictionary(
                    x => x.Key,
                    x => x.Value
                )
            );
            //e.BadgeNotification
            //await this.serviceProvider.Resolve<IPushDelegate>()?.OnReceived(e.RawNotification.Content);
            //this.CurrentRegistrationToken = e.RawNotification.Ur
            //this.CurrentRegistrationTokenDate = DateTime.UtcNow;
            //e.Cancel = true;
        });
    }
}
