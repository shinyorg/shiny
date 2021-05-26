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
using Shiny.Notifications;


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

        protected abstract void OnStart(Intent? intent);
        protected abstract void OnStop();

        protected TService? Service { get; private set; }
        protected IList<TDelegate>? Delegates { get; private set; }
        protected IAndroidContext? Context { get; private set; }
        protected AndroidNotificationManager? AndroidNotifications { get; private set; }


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
        public static string NotificationChannelId { get; set; } = "Background";


        protected virtual void Start(Intent? intent)
        {
            this.DestroyWith = new CompositeDisposable();
            this.Service = this.Resolve<TService>();
            this.Delegates = this.ResolveAll<TDelegate>().ToList();
            this.Context = this.Resolve<IAndroidContext>();
            this.AndroidNotifications = this.Resolve<AndroidNotificationManager>();

            if (this.Context.IsMinApiLevel(26))
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

            if (this.Context!.IsMinApiLevel(26))
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
            var nm = this.AndroidNotifications.NativeManager;
            if (nm.GetNotificationChannel(NotificationChannelId) != null)
                return;

            nm.CreateNotificationChannel(new NotificationChannel(
                NotificationChannelId,
                "Background",
                NotificationImportance.Default
            ));
        }


        protected virtual void SendNotification()
        {
            this.builder ??= new NotificationCompat.Builder(this.Context.AppContext)
                            .SetChannelId(NotificationChannelId)
                            .SetSmallIcon(this.AndroidNotifications.GetSmallIconResource(null))
                            .SetOngoing(true);

            this.builder
                .SetProgress(
                    this.Service.Total,
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
                this.AndroidNotifications.NativeManager.Notify(this.notificationId.Value, this.builder.Build());
            }
        }
    }
}
