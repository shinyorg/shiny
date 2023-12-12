using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;

namespace Shiny;


public abstract class ShinyAndroidForegroundService : Service
{
    public static string NotificationChannelId { get; set; } = "Service";
    static int startNotificationId = 7999;


    int? privateNotificationId;
    protected int NotificationId
    {
        get
        {
            this.privateNotificationId ??= ++startNotificationId;
            return this.privateNotificationId.Value;
        }
    }

    protected NotificationCompat.Builder? Builder { get; private set; }

    protected virtual ForegroundService StartForegroundServiceType => ForegroundService.TypeNone;
    protected T GetService<T>() => Host.GetService<T>()!;
    protected IEnumerable<T> GetServices<T>() => Host.ServiceProvider.GetServices<T>();
    protected CompositeDisposable? DestroyWith { get; private set; }
    protected NotificationManagerCompat? NotificationManager { get; private set; }
    protected bool StopWithTask { get; private set; }

    protected abstract void OnStart(Intent? intent);
    protected abstract void OnStop();

    ILogger? logger;
    protected ILogger Logger => this.logger ??= Host.Current.Logging.CreateLogger(this.GetType()!);

    AndroidPlatform? platform;
    protected AndroidPlatform Platform => this.platform ??= this.GetService<AndroidPlatform>();


    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        this.Logger.LogDebug($"Foreground Service OnStartCommand - Action: {intent?.Action} - Notification ID: {this.NotificationId}");
        switch (intent?.Action)
        {
            case AndroidPlatform.ActionServiceStart:
                this.StopWithTask = intent.GetBooleanExtra(AndroidPlatform.IntentActionStopWithTask, false);
                this.Start(intent);
                break;

            case AndroidPlatform.ActionServiceStop:
                this.Stop();
                break;

            default:
                this.Logger.LogDebug($"Invalid Intent Action - {intent?.Action}");
                break;
        }

        return StartCommandResult.Sticky;
    }


    public override IBinder? OnBind(Intent? intent) => null;
    public override void OnTaskRemoved(Intent? rootIntent)
    {
        this.Logger.LogDebug($"Foreground Service OnTaskRemoved - StopWithTask: {this.StopWithTask}");
        if (this.StopWithTask)
            this.Stop();
    }


    protected virtual void Start(Intent? intent)
    {
        this.NotificationManager = NotificationManagerCompat.From(this.Platform.AppContext);
        this.DestroyWith = new CompositeDisposable();

        this.EnsureChannel();
        this.Builder = this.CreateNotificationBuilder();

        this.Logger.LogDebug("Starting Foreground Notification: " + this.NotificationId);
        var notification = this.Builder.Build();
        notification.Flags |= NotificationFlags.ForegroundService;

        ServiceCompat.StartForeground(this, this.NotificationId, notification, (int)this.StartForegroundServiceType);
        this.Logger.LogDebug("Started Foreground Service");

        this.OnStart(intent);
    }


    protected void Stop()
    {
        this.Logger.LogDebug($"Calling for foreground service stop.  Notification ID: {this.NotificationId}");
        this.DestroyWith?.Dispose();
        this.DestroyWith = null;

        ServiceCompat.StopForeground(this, ServiceCompat.StopForegroundRemove);
        this.StopSelf();

        this.Logger.LogDebug("Foreground service stopped successfully");
        this.OnStop();
    }


    protected virtual void EnsureChannel()
    {
        if (this.NotificationManager!.GetNotificationChannel(NotificationChannelId) != null)
            return;

        var channel = new NotificationChannel(
            NotificationChannelId,
            NotificationChannelId,
            NotificationImportance.Default
        );
        channel.SetShowBadge(false);
        this.NotificationManager.CreateNotificationChannel(channel);
    }


    protected virtual NotificationCompat.Builder CreateNotificationBuilder()
    {
        var build = new NotificationCompat.Builder(this.Platform.AppContext, NotificationChannelId)
            .SetSmallIcon(this.Platform.GetNotificationIconResource())
            .SetForegroundServiceBehavior((int)NotificationForegroundService.Immediate)
            .SetOngoing(true)
            .SetOnlyAlertOnce(true)
            .SetTicker("...")
            .SetContentTitle("Shiny Service")
            .SetContentText("Shiny service is continuing to process data in the background");

        return build;
    }
}


public abstract class ShinyAndroidForegroundService<TService, TDelegate> : ShinyAndroidForegroundService
{
    protected TService Service { get; private set; } = default!;
    protected IList<TDelegate> Delegates { get; private set; } = null!;


    protected override void Start(Intent? intent)
    {
        this.Service = this.GetService<TService>();
        this.Delegates = this.GetServices<TDelegate>().ToList();

        base.Start(intent);
    }


    protected override NotificationCompat.Builder CreateNotificationBuilder()
    {
        var build = base.CreateNotificationBuilder();
        this.Delegates!
            .OfType<IAndroidForegroundServiceDelegate>()
            .ToList()
            .ForEach(x => x.Configure(build));

        return build;
    }
}
