using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Windows.Foundation;
using Windows.Networking.PushNotifications;
using Windows.ApplicationModel.Background;
using Shiny.Settings;


namespace Shiny.Push
{
    public class PushManager : AbstractPushManager, IShinyStartupTask
    {
        readonly IServiceProvider serviceProvider;
        PushNotificationChannel channel;


        public PushManager(IServiceProvider serviceProvider, ISettings settings) : base(settings)
        {
            this.serviceProvider = serviceProvider;
        }


        public void Start()
        {
            // TODO: if push is enabled, start timer and check channel expiration
            // if push already registered, start it up automagically here
            //e.BadgeNotification
            //await this.serviceProvider.Resolve<IPushDelegate>()?.OnReceived(e.RawNotification.Content);
            //this.CurrentRegistrationToken = e.RawNotification.Ur
            //this.CurrentRegistrationTokenDate = DateTime.UtcNow;
            //e.Cancel = true;
            UwpShinyHost.RegisterBackground<PushNotificationBackgroundTaskProcessor>(
                builder => builder.SetTrigger(new PushNotificationTrigger())
            );
        }


        public override IObservable<IDictionary<string, string>> WhenReceived() => Observable.Create<IDictionary<string, string>>(async ob =>
        {
            var handler = new TypedEventHandler<PushNotificationChannel, PushNotificationReceivedEventArgs>((sender, args) =>
            {
                var headers = args
                    .RawNotification?
                    .Headers?
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value
                    ) ?? new Dictionary<string, string>(0);

                ob.OnNext(headers);
            });
            this.channel.PushNotificationReceived += handler;
            return () => this.channel.PushNotificationReceived -= handler;
        });


        public override async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            var channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            //channel.ExpirationTime - persist and deal with this


            return new PushAccessState(AccessState.Available, channel.Uri);
        }


        public override Task UnRegister()
        {
            this.channel?.Close();
            return Task.CompletedTask;
        }
    }
}
