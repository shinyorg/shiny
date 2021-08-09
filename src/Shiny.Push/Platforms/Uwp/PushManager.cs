using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Windows.Foundation;
using Windows.Networking.PushNotifications;
using Windows.ApplicationModel.Background;
using Microsoft.Extensions.Logging;
using Shiny.Stores;


namespace Shiny.Push
{
    public class PushManager : IPushManager, IShinyStartupTask
    {
        readonly PushContainer container;
        readonly IKeyValueStore store;
        readonly ILogger logger;
        PushNotificationChannel channel;


        public PushManager(PushContainer container,
                           IKeyValueStoreFactory storeFactory,
                           ILogger<IPushManager> logger)
        {
            this.container = container;
            this.store = storeFactory.DefaultStore;
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
            get => this.store.Get<DateTime?>(nameof(this.CurrentRegistrationExpiryDate));
            set => this.store.SetOrRemove(nameof(this.CurrentRegistrationExpiryDate), value);
        }
        public DateTime? CurrentRegistrationTokenDate => this.container.CurrentRegistrationTokenDate;
        public string? CurrentRegistrationToken => this.container.CurrentRegistrationToken;


        public IObservable<PushNotification> WhenReceived() => Observable.Create<PushNotification>(ob =>
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


        public async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            this.channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            this.CurrentRegistrationExpiryDate = this.channel.ExpirationTime.DateTime.ToUniversalTime();
            this.container.SetCurrentToken(this.channel.Uri, false);
            this.InitializeExpirationTimer();
            return new PushAccessState(AccessState.Available, channel.Uri);
        }


        public Task UnRegister()
        {
            this.channel?.Close();
            this.expiryTimer?.Dispose();
            this.container.ClearRegistration();
            this.CurrentRegistrationExpiryDate = null;

            return Task.CompletedTask;
        }


        IDisposable expiryTimer;
        void InitializeExpirationTimer()
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
