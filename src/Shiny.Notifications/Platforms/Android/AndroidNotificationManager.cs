using System;
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


    public AndroidNotificationManager(
        AndroidPlatform platform,
        IChannelManager channelManager
    )
    {
        this.platform = platform;
        this.NativeManager = NotificationManagerCompat.From(this.platform.AppContext);
        this.channelManager = channelManager;
    }


    public virtual async Task Send(Notification notification)
    {
        var channel = await this.channelManager.Get(notification.Channel!);
        //await this.Services.Platform.TrySetImage(notification.ImageUri, builder);
        var builder = this.CreateNativeBuilder(notification, channel!);
        this.SendNative(notification.Id, builder.Build());
    }


    public Android.App.Notification CreateNativeNotification(Notification notification, Channel channel)
        => this.CreateNativeBuilder(notification, channel).Build();


    public virtual NotificationCompat.Builder CreateNativeBuilder(Notification notification, Channel channel)
    {
        var builder = new NotificationCompat.Builder(this.platform.AppContext, channel.Identifier);
        // TODO: apply customizers here including default
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


    public virtual void SendNative(int id, Android.App.Notification notification)
        => this.NativeManager.Notify(id, notification);
}
