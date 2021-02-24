using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Shiny.Notifications;


namespace Shiny
{
    public abstract class ShinyAndroidForegroundService<TService, TDelegate> : Service where TService : IShinyForegroundManager
    {
        static int idCount = 7999;
        int? notificationId;

        readonly Lazy<IMessageBus> messageBus = ShinyHost.LazyResolve<IMessageBus>();

        protected void Publish<T>(T args) => this.messageBus.Value.Publish(args);
        protected void Publish<T>(string name, T args) => this.messageBus.Value.Publish(name, args);
        protected void Publish(string name) => this.messageBus.Value.Publish(name);

        protected T Resolve<T>() => ShinyHost.Resolve<T>();
        protected IEnumerable<T> ResolveAll<T>() => ShinyHost.ResolveAll<T>();
        protected Lazy<T> ResolveLazy<T>() => ShinyHost.LazyResolve<T>();
        protected CompositeDisposable DestroyWith { get; } = new CompositeDisposable();

        //protected virtual void LogError<T>(Exception exception, string message)
        //    => ShinyHost.LoggerFactory.CreateLogger<T>().LogError(exception, message);


        protected abstract void OnStart(Intent? intent);
        protected virtual void OnStop() { }

        protected TService? Service { get; private set; }
        protected IList<TDelegate>? Delegates { get; private set; }
        protected IAndroidContext? Context { get; private set; }
        protected AndroidNotificationManager? AndroidNotifications { get; private set; }


        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            this.Service = this.Resolve<TService>();
            this.Delegates = this.ResolveAll<TDelegate>().ToList();
            this.Context = this.Resolve<IAndroidContext>();
            this.AndroidNotifications = this.Resolve<AndroidNotificationManager>();

            if (this.Context.IsMinApiLevel(26))
            {
                this.Service
                    .WhenAnyProperty()
                    .Subscribe(_ => this.SetNotification())
                    .DisposedBy(this.DestroyWith);

                this.SetNotification();
            }
            this.OnStart(intent);
            return StartCommandResult.Sticky;
        }


        public override void OnDestroy()
        {
            this.DestroyWith.Dispose();

            if (this.Context!.IsMinApiLevel(26))
                this.StopForeground(true);

            this.notificationId = null;
            this.OnStop();
            base.OnDestroy();
        }


        public override IBinder? OnBind(Intent? intent) => null;



        NotificationCompat.Builder builder;
        void SetNotification()
        {
            this.builder ??= new NotificationCompat.Builder(this.Context.AppContext)
                .SetChannelId(Channel.Default.Identifier)
                .SetSmallIcon(this.AndroidNotifications.GetSmallIconResource(null))
                .SetOngoing(true);
//                .SetContentIntent(this.AndroidNotifications.GetLaunchPendingIntent(null));

            this.builder.SetProgress(
                this.Service.Total,
                this.Service.Progress,
                this.Service.IsIndeterministic
            );

            if (this.Service.Title.IsEmpty())
                this.builder.SetContentTitle(this.Service.Title);

            if (!this.Service.Message.IsEmpty())
                this.builder.SetContentText(this.Service.Message);

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