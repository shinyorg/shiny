using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Extensions.Logging;


namespace Shiny
{
    public abstract class ShinyAndroidForegroundService<TService, TDelegate> : Service where TService : IShinyForegroundManager
    {
        static int idCount = 7999;
        int? notificationId;

        protected T Resolve<T>() => ShinyHost.Resolve<T>();
        protected IEnumerable<T> ResolveAll<T>() => ShinyHost.ResolveAll<T>();
        protected Lazy<T> ResolveLazy<T>() => ShinyHost.LazyResolve<T>();
        protected CompositeDisposable? DestroyWith { get; private set; }
        protected NotificationManagerCompat? NotificationManager { get; private set; }

        protected abstract void OnStart(Intent? intent);
        protected abstract void OnStop();

        protected TService? Service { get; private set; }
        protected IList<TDelegate>? Delegates { get; private set; }

        IPlatform? platform;
        protected IPlatform Platform => this.platform ??= this.Resolve<IPlatform>();


        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            var action = intent?.Action ?? AndroidPlatform.ActionServiceStart;
            switch (action)
            {
                case AndroidPlatform.ActionServiceStart:
                    this.Start(intent);
                    break;

                case AndroidPlatform.ActionServiceStop:
                    this.Stop();
                    break;
            }

            return StartCommandResult.Sticky;
        }


        public override IBinder? OnBind(Intent? intent) => null;
        public static string NotificationChannelId { get; set; } = "Service";


        protected virtual void Start(Intent? intent)
        {
            this.NotificationManager = NotificationManagerCompat.From(this.Platform.AppContext);
            this.DestroyWith = new CompositeDisposable();
            this.Service = this.Resolve<TService>();
            this.Delegates = this.ResolveAll<TDelegate>().ToList();

            if (this.Platform.IsMinApiLevel(26))
            {
                this.Service
                    .WhenAnyProperty()
                    .Skip(1)
                    .Throttle(TimeSpan.FromMilliseconds(400))
                    .Subscribe(_ => this.SetNotification())
                    .DisposedBy(this.DestroyWith);

                this.SetNotification();
            }
            this.OnStart(intent);
        }


        protected virtual void Stop()
        {
            if (this.DestroyWith == null)
                return;

            this.DestroyWith?.Dispose();
            this.DestroyWith = null;

            if (this.Platform!.IsMinApiLevel(26))
                this.StopForeground(true);

            this.StopSelf();
            this.notificationId = null;
            this.OnStop();
        }


        NotificationCompat.Builder? builder;
        protected virtual void SetNotification()
        {
            try
            {
                this.EnsureChannel();
                this.SendNotification();
            }
            catch (Exception ex)
            {
                ShinyHost
                    .LoggerFactory
                    .CreateLogger(this.GetType().AssemblyQualifiedName)
                    .LogError(ex, "Failed to send notification - your android foreground service will fail because of this error");
            }
        }


        protected virtual void EnsureChannel()
        {
            if (this.NotificationManager.GetNotificationChannel(NotificationChannelId) != null)
                return;

            var channel = new NotificationChannel(
                NotificationChannelId,
                NotificationChannelId,
                NotificationImportance.Default
            );
            channel.SetShowBadge(false);
            this.NotificationManager.CreateNotificationChannel(channel);
        }


        protected virtual void SendNotification()
        {
            this.builder ??= new NotificationCompat.Builder(this.Platform.AppContext, NotificationChannelId)
                .SetSmallIcon(this.GetNotificationIcon())
                .SetOngoing(true);

            this.builder
                .SetProgress(
                    this.Service!.Total,
                    this.Service.Progress,
                    this.Service.IsIndeterministic
                )
                .SetContentTitle(this.Service.Title ?? "Shiny Service")
                .SetTicker("..")
                .SetContentText(this.Service.Message ?? "Shiny service is continuing to process data in the background");

            if (this.notificationId == null)
            {
                this.notificationId = ++idCount;
                this.StartForeground(this.notificationId.Value, this.builder.Build());
            }
            else
            {
                this.NotificationManager.Notify(this.notificationId.Value, this.builder.Build());
            }
        }


        protected virtual int GetNotificationIcon()
        {
            var id = this.Platform.GetResourceIdByName("notification");
            if (id > 0)
                return id;

            id = this.Platform.AppContext.ApplicationInfo?.Icon ?? 0;
            if (id > 0)
                return id;

            throw new InvalidOperationException("Unable to find notification icon for Shiny foreground service - ensure you have your application icon set or a drawable resource named notification");
        }
    }
}
