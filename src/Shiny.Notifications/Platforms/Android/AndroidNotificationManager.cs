using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using AndroidX.Core.App;
using Java.Lang;

namespace Shiny.Notifications;


public class AndroidNotificationManager
{
    public NotificationManagerCompat NativeManager { get; }
    readonly AndroidPlatform platform;
    readonly IChannelManager channelManager;
    readonly IEnumerable<INotificationCustomizer> customizers;


    public AndroidNotificationManager(
        AndroidPlatform platform,
        IChannelManager channelManager,
        IEnumerable<INotificationCustomizer> customizers
    )
    {
        this.platform = platform;
        this.NativeManager = NotificationManagerCompat.From(this.platform.AppContext);
        this.channelManager = channelManager;
        this.customizers = customizers;
    }


    public virtual async Task Send(Notification notification)
    {
        var channel = await this.channelManager.Get(notification.Channel!).ConfigureAwait(false);
        var builder = await this.CreateNativeBuilder(notification, channel!).ConfigureAwait(false);
        this.NativeManager.Notify(notification.Id, builder.Build());
    }


    public virtual async Task<NotificationCompat.Builder> CreateNativeBuilder(Notification notification, Channel channel)
    {
        var builder = new NotificationCompat.Builder(this.platform.AppContext, channel.Identifier);
        foreach (var customizer in this.customizers)
            await customizer.Customize(notification, channel, builder).ConfigureAwait(false);

        return builder;
    }


    public void SetAlarm(Notification notification)
    {
        var pendingIntent = this.GetAlarmPendingIntent(notification);
        var triggerTime = (notification.ScheduleDate!.Value.ToUniversalTime() - DateTime.UtcNow).TotalMilliseconds;
        var androidTriggerTime = JavaSystem.CurrentTimeMillis() + (long)triggerTime;
        this.Alarms.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, androidTriggerTime, pendingIntent);
    }


    public void CancelAlarm(Notification notification)
    {
        this.Alarms.Cancel(this.GetAlarmPendingIntent(notification));
    }


    protected virtual PendingIntent GetAlarmPendingIntent(Notification notification)
        => this.platform.GetBroadcastPendingIntent<ShinyNotificationBroadcastReceiver>(
            ShinyNotificationBroadcastReceiver.AlarmIntentAction,
            PendingIntentFlags.UpdateCurrent,
            0,
            intent => intent.PutExtra(AndroidNotificationProcessor.IntentNotificationKey, notification.Id)
        );


    AlarmManager? alarms;
    public AlarmManager Alarms => this.alarms ??= this.platform.GetSystemService<AlarmManager>(Context.AlarmService);
}
