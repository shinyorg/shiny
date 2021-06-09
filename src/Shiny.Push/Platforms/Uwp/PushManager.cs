using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Windows.Foundation;
using Windows.Networking.PushNotifications;
using Windows.ApplicationModel.Background;
using Shiny.Infrastructure;
using Microsoft.Extensions.Logging;


namespace Shiny.Push
{
    public class PushManager : AbstractPushManager, IShinyStartupTask
    {
        readonly ILogger logger;
        PushNotificationChannel channel;


        public PushManager(ShinyCoreServices services, ILogger<IPushManager> logger) : base(services)
        {
            this.logger = logger;
         }


        public async void Start()
        {
            try
            {
                UwpPlatform.RegisterBackground<PushNotificationBackgroundTaskProcessor>(
                    builder => builder.SetTrigger(new PushNotificationTrigger())
                );
                if (this.CurrentRegistrationExpiryDate != null)
                    await this.RequestAccess();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Unable to register push");
            }
        }


        DateTime? CurrentRegistrationExpiryDate
        {
            get => this.Settings.Get<DateTime?>(nameof(this.CurrentRegistrationExpiryDate));
            set => this.Settings.SetOrRemove(nameof(this.CurrentRegistrationExpiryDate), value);
        }


        public override IObservable<PushNotification> WhenReceived() => Observable.Create<PushNotification>(ob =>
        {
            var handler = new TypedEventHandler<PushNotificationChannel, PushNotificationReceivedEventArgs>((sender, args) =>
            {
                var headers = ExtractHeaders(args);
                var pr = new PushNotification(headers, null);
                ob.OnNext(pr);
            });
            this.channel.PushNotificationReceived += handler;
            return () => this.channel.PushNotificationReceived -= handler;
        });


        public override async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            this.channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            this.CurrentRegistrationExpiryDate = this.channel.ExpirationTime.DateTime.ToUniversalTime();
            this.CurrentRegistrationToken = this.channel.Uri;
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;
            this.InitializeExpirationTimer();
            return new PushAccessState(AccessState.Available, channel.Uri);
        }


        public override Task UnRegister()
        {
            this.channel?.Close();
            this.expiryTimer?.Dispose();
            return Task.CompletedTask;
        }


        IDisposable expiryTimer;
        protected virtual void InitializeExpirationTimer()
        {
            this.expiryTimer = Observable
                .Interval(TimeSpan.FromMinutes(15))
                .Where(x => this.CurrentRegistrationExpiryDate != null && this.CurrentRegistrationExpiryDate < DateTime.UtcNow)
                .Select(x => Observable.FromAsync(this.RequestAccess))
                .Subscribe();
        }


        public static IDictionary<string, string> ExtractHeaders(PushNotificationReceivedEventArgs args)
        {
            IDictionary<string, string> headers = new Dictionary<string, string>();

            if (args.RawNotification != null)
            {
                if (args.RawNotification.Headers != null)
                    headers = args.RawNotification.Headers.ToDictionary(x => x.Key, x => x.Value);
            }
            else if (args.ToastNotification != null)
            {
                if (args.ToastNotification.Data?.Values != null)
                    headers = args.ToastNotification.Data.Values;
            }
            else if (args.TileNotification != null)
            {
                headers.Add("Tag", args.TileNotification.Tag);
            }
            return headers;
        }
    }
}
