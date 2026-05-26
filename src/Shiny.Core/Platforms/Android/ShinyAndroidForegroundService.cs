using System.Collections.Generic;
using System.Linq;
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
    protected DisposableCollection? DestroyWith { get; private set; }
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
        // Android kills the process with ForegroundServiceDidNotStartInTimeException if
        // startForeground() isn't called within ~5s of startForegroundService(). Promote
        // to foreground before any DI lookup or action dispatch that could throw or stall.
        this.PromoteToForegroundSafely();
        this.TryFlushPendingLogs();

        try
        {
            this.Logger.LogDebug($"Foreground Service OnStartCommand - Action: {intent?.Action} - Notification ID: {this.NotificationId}");

            // null intent means Android re-created the service after a process kill -
            // resume as if it were a fresh start so tracking can re-arm itself.
            switch (intent?.Action ?? AndroidPlatform.ActionServiceStart)
            {
                case AndroidPlatform.ActionServiceStart:
                    this.StopWithTask = intent?.GetBooleanExtra(AndroidPlatform.IntentActionStopWithTask, false) ?? false;
                    this.Start(intent);
                    break;

                case AndroidPlatform.ActionServiceStop:
                    this.Stop();
                    break;

                default:
                    this.Logger.LogDebug($"Unknown Intent Action - {intent?.Action}");
                    break;
            }
        }
        catch (System.Exception ex)
        {
            // Already in foreground at this point - swallow so the process survives.
            try { this.Logger.LogError(ex, "Foreground service OnStartCommand failed"); } catch { }
        }

        return StartCommandResult.RedeliverIntent;
    }


    bool isPromoted;
    void PromoteToForegroundSafely()
    {
        if (this.isPromoted)
            return;

        try
        {
            var ctx = this.ApplicationContext!;
            var nm = NotificationManagerCompat.From(ctx);

            if (nm.GetNotificationChannel(NotificationChannelId) == null)
            {
                var channel = new NotificationChannel(NotificationChannelId, NotificationChannelId, NotificationImportance.Default);
                channel.SetShowBadge(false);
                nm.CreateNotificationChannel(channel);
            }

            // Resolve icon without DI: prefer drawable/notification, fall back to app icon.
            var iconId = ctx.Resources?.GetIdentifier("notification", "drawable", ctx.PackageName) ?? 0;
            if (iconId <= 0)
                iconId = ctx.ApplicationInfo?.Icon ?? Android.Resource.Drawable.SymDefAppIcon;

            var placeholder = new NotificationCompat.Builder(ctx, NotificationChannelId)
                .SetSmallIcon(iconId)
                .SetForegroundServiceBehavior((int)NotificationForegroundService.Immediate)
                .SetOngoing(true)
                .SetOnlyAlertOnce(true)
                .SetContentTitle("Shiny Service")
                .SetContentText("Shiny service is continuing to process data in the background")
                .Build();
            placeholder.Flags |= NotificationFlags.ForegroundService;

            ServiceCompat.StartForeground(this, this.NotificationId, placeholder, (int)this.StartForegroundServiceType);
            this.isPromoted = true;
            this.pendingPromoteLog = ($"{this.GetType().Name} promoted to foreground (id={this.NotificationId}, type={this.StartForegroundServiceType})", null);
        }
        catch (System.Exception ex)
        {
            // Capture for deferred logging via ILogger once the Host is ready.
            // The original StartForeground in Start() may still succeed for non-restart paths.
            this.pendingPromoteLog = ($"{this.GetType().Name} failed to promote to foreground", ex);
        }
    }


    (string Message, System.Exception? Exception)? pendingPromoteLog;
    void TryFlushPendingLogs()
    {
        if (this.pendingPromoteLog == null)
            return;

        if (!Shiny.Hosting.Host.IsInitialized)
            return;

        try
        {
            var (msg, ex) = this.pendingPromoteLog.Value;
            if (ex == null)
                this.Logger.LogInformation(msg);
            else
                this.Logger.LogError(ex, msg);

            this.pendingPromoteLog = null;
        }
        catch
        {
            // Logger resolution can still fail mid-boot; leave queued for next attempt.
        }
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
        this.TryFlushPendingLogs();
        this.NotificationManager = NotificationManagerCompat.From(this.Platform.AppContext);
        this.DestroyWith = new DisposableCollection();

        this.EnsureChannel();
        this.Builder = this.CreateNotificationBuilder();

        this.Logger.LogDebug("Starting Foreground Notification: " + this.NotificationId);
        var notification = this.Builder.Build();
        notification.Flags |= NotificationFlags.ForegroundService;

        // Replace the placeholder posted by PromoteToForegroundSafely with the configured one.
        ServiceCompat.StartForeground(this, this.NotificationId, notification, (int)this.StartForegroundServiceType);
        this.isPromoted = true;
        this.Logger.LogDebug("Started Foreground Service");

        this.OnStart(intent);
    }


    protected void Stop()
    {
        this.TryFlushPendingLogs();
        this.Logger.LogDebug($"Calling for foreground service stop.  Notification ID: {this.NotificationId}");
        this.DestroyWith?.Dispose();
        this.DestroyWith = null;

        ServiceCompat.StopForeground(this, ServiceCompat.StopForegroundRemove);
        this.isPromoted = false;
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
