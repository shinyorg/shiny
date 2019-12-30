using System;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using Shiny.Logging;


namespace Shiny.Push
{
    public class PushManager : IPushManager
    {
        readonly IServiceProvider serviceProvider;
        public PushManager(IServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;


        public async Task<PushAccessState> RequestAccess()
        {
            var channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            //channel.ExpirationTime - persist and deal with this
            channel.PushNotificationReceived += this.OnPushNotification;
            return new PushAccessState(AccessState.Available, channel.Uri);
        }


        void OnPushNotification(PushNotificationChannel sender, PushNotificationReceivedEventArgs e) => Log.SafeExecute(async () =>
        {
            await this.serviceProvider.Resolve<IPushDelegate>()?.OnReceived(e.RawNotification.Content);
            e.Cancel = true;
        });
    }
}
