using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;


namespace Shiny
{
    public abstract class ShinyAndroidForegroundService<TService, TDelegate> : Service where TService : IShinyForegroundManager
    {
        static int idCount = 0;
        int? notificationId;

        NotificationCompat.Builder? notificationBuilder;
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


        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
             
            this.Service = this.Resolve<TService>();
            this.Delegates = this.ResolveAll<TDelegate>().ToList();
            this.Context = this.Resolve<IAndroidContext>();

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

            if (this.Context.IsMinApiLevel(26))
                this.StopForeground(true);

            this.OnStop();
            base.OnDestroy();
        }


        public override IBinder? OnBind(Intent? intent) => null;


        void SetNotification()
        {
            //var pendingIntent = this.GetLaunchPendingIntent(notification);
            this.notificationBuilder ??= new NotificationCompat.Builder(this.Context.AppContext);
            this.notificationBuilder
                .SetContentTitle("")
                .SetContentText("")
                //        builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(notification.Message));
                .SetTicker("")
                .SetContentInfo("")
                .SetOngoing(true)
                //.SetAutoCancel(false)
                .SetCategory(Notification.CategoryService);
            // TODO: channel needs to already exist
            //    builder.SetChannelId(notification.Channel ?? Channel.Default.Identifier);
            //    .SetSmallIcon(this.GetSmallIconResource(notification))


            if (this.notificationId == null)
            {
                this.notificationId = ++idCount;
                this.StartForeground(this.notificationId.Value, this.notificationBuilder.Build());
            }
            else
            {
                var manager = NotificationManagerCompat.From(this.Context.AppContext);
                manager.Notify(this.notificationId.Value, this.notificationBuilder.Build());
            }
        }
    }
}