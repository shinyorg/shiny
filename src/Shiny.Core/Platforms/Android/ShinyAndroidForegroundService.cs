using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;

namespace Shiny;


public abstract class ShinyAndroidForegroundService : Service
{
    public static string NotificationChannelId { get; set; } = "Service";
    static int idCount = 7999;
    int? notificationId;

    protected T GetService<T>() => Host.GetService<T>()!;
    protected IEnumerable<T> GetServices<T>() => Host.ServiceProvider.GetServices<T>();
    protected CompositeDisposable? DestroyWith { get; private set; }
    protected NotificationManagerCompat? NotificationManager { get; private set; }
    protected bool StopWithTask { get; private set; }

    protected abstract void OnStart(Intent? intent);
    protected abstract void OnStop();


    ILogger? logger;
    protected ILogger Logger => this.logger ??= Host.Current.Logging.CreateLogger(this.GetType()!.AssemblyQualifiedName!);

    AndroidPlatform? platform;
    protected AndroidPlatform Platform => this.platform ??= this.GetService<AndroidPlatform>();


    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        this.notificationId = idCount++;
        var action = intent?.Action ?? AndroidPlatform.ActionServiceStart;
        switch (action)
        {
            case AndroidPlatform.ActionServiceStart:
                this.StopWithTask = intent?.GetBooleanExtra("StopWithTask", false) ?? false;
                this.Start(intent);
                break;

            case AndroidPlatform.ActionServiceStop:
                this.Stop();
                break;
        }

        return StartCommandResult.Sticky;
    }


    public override IBinder? OnBind(Intent? intent) => null;
    public override void OnTaskRemoved(Intent? rootIntent)
    {
        if (this.StopWithTask)
            this.Stop();
    }


    protected virtual void Start(Intent? intent)
    {
        this.NotificationManager = NotificationManagerCompat.From(this.Platform.AppContext);
        this.DestroyWith = new CompositeDisposable();

        if (OperatingSystemShim.IsAndroidVersionAtLeast(26))
            this.SetNotification();

        this.OnStart(intent);
    }


    protected void Stop()
    {
        if (this.DestroyWith == null)
            return;

        this.DestroyWith?.Dispose();
        this.DestroyWith = null;

        if (OperatingSystemShim.IsAndroidVersionAtLeast(26))
            this.StopForeground(StopForegroundFlags.Detach);

        this.StopSelf();
        this.notificationId = null;
        this.OnStop();
    }


    protected NotificationCompat.Builder? Builder { get; private set; }
    protected virtual void SetNotification()
    {
        try
        {
            this.EnsureChannel();
            this.Builder = this.CreateNotificationBuilder();

            this.notificationId = ++idCount;
            this.StartForeground(this.notificationId.Value, this.Builder.Build());
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to send notification - your android foreground service will fail because of this error");
        }
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
