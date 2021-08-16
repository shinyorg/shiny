using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Shiny.Push.Infrastructure;
using Windows.Networking.PushNotifications;


namespace Shiny.Push
{
    public class NativeAdapter : INativeAdapter
    {
        PushNotificationChannel? channel;
        IDisposable? expiryTimer;

        // START
        //            try
        //            {
        //                UwpPlatform.RegisterBackground<PushNotificationBackgroundTaskProcessor>(
        //                    builder => builder.SetTrigger(new PushNotificationTrigger())
        //                );
        //                if (this.CurrentRegistrationExpiryDate != null)
        //                    await this.RequestAccess();
        //            }
        //            catch (Exception ex)
        //            {
        //                this.logger.LogError(ex, "Unable to register push");
        //            }

        public Func<PushNotification, Task>? OnReceived
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }


        public Func<PushNotificationResponse, Task>? OnEntry
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }


        public Func<string, Task>? OnTokenRefreshed
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }


        public async Task<PushAccessState> RequestAccess()
        {
            this.channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            var expiryDate = this.channel.ExpirationTime.DateTime.ToUniversalTime();
            this.expiryTimer = Observable
                .Interval(TimeSpan.FromMinutes(15))
                .Where(x => expiryDate < DateTime.UtcNow && this.OnTokenRefreshed != null)
                .SubscribeAsync(async _ =>
                {
                    var access = await this.RequestAccess().ConfigureAwait(false);
                    if (!access.RegistrationToken?.Equals(this.channel.Uri) ?? true)
                        await this.OnTokenRefreshed!.Invoke(this.channel.Uri).ConfigureAwait(false);
                });

            return new PushAccessState(AccessState.Available, this.channel.Uri);
        }


        public Task UnRegister()
        {
            this.channel?.Close();
            this.expiryTimer?.Dispose();
            return Task.CompletedTask;
        }
    }
}
//        public static IDictionary<string, string> ExtractHeaders(PushNotificationReceivedEventArgs args)
//        {
//            IDictionary<string, string> headers = new Dictionary<string, string>();

//            if (args.RawNotification != null)
//            {
//                if (args.RawNotification.Headers != null)
//                    headers = args.RawNotification.Headers.ToDictionary(x => x.Key, x => x.Value);
//            }
//            else if (args.ToastNotification != null)
//            {
//                if (args.ToastNotification.Data?.Values != null)
//                    headers = args.ToastNotification.Data.Values;
//            }
//            else if (args.TileNotification != null)
//            {
//                headers.Add("Tag", args.TileNotification.Tag);
//            }
//            return headers;
//        }




//        public IObservable<PushNotification> WhenReceived() => Observable.Create<PushNotification>(ob =>
//        {
//            var handler = new TypedEventHandler<PushNotificationChannel, PushNotificationReceivedEventArgs>((sender, args) =>
//            {
//                var headers = ExtractHeaders(args);
//                var pr = new PushNotification(headers, null);
//                ob.OnNext(pr);
//            });
//            this.channel.PushNotificationReceived += handler;
//            return () => this.channel.PushNotificationReceived -= handler;
//        });
