using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Java.Lang;
using Shiny.Infrastructure;


namespace Shiny.Notifications
{
    public class AndroidNotificationManager
    {
        protected ShinyCoreServices Services { get; }
        public NotificationManagerCompat NativeManager { get; }
        readonly IChannelManager channelManager;


        public AndroidNotificationManager(ShinyCoreServices services, IChannelManager channelManager)
        {
            this.Services = services;
            this.NativeManager = NotificationManagerCompat.From(this.Services.Platform.AppContext);
            this.channelManager = channelManager;
        }


        public virtual async Task Send(Notification notification)
        {
            var channel = await this.channelManager.Get(notification.Channel!);
            var builder = this.CreateNativeBuilder(notification, channel!);
            this.SendNative(notification.Id, builder.Build());
        }


        public Android.App.Notification CreateNativeNotification(Notification notification, Channel channel)
            => this.CreateNativeBuilder(notification, channel).Build();


        public virtual NotificationCompat.Builder CreateNativeBuilder(Notification notification, Channel channel)
        {
            var builder = new NotificationCompat.Builder(this.Services.Platform.AppContext, channel.Identifier)
                .SetContentTitle(notification.Title)
                .SetSmallIcon(this.Services.Platform.GetSmallIconResource(notification.Android.SmallIconResourceName))
                .SetAutoCancel(notification.Android.AutoCancel)
                .SetOngoing(notification.Android.OnGoing);

            if (!notification.LocalAttachmentPath.IsEmpty())
                this.Services.Platform.TrySetImage(notification.LocalAttachmentPath!, builder);

            if (!notification.Thread.IsEmpty())
                builder.SetGroup(notification.Thread);

            this.ApplyLaunchIntent(builder, notification);
            if (!notification.Android.ContentInfo.IsEmpty())
                builder.SetContentInfo(notification.Android.ContentInfo);

            if (!notification.Android.Ticker.IsEmpty())
                builder.SetTicker(notification.Android.Ticker);

            if (notification.Android.UseBigTextStyle)
                builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(notification.Message));
            else
                builder.SetContentText(notification.Message);

            this.Services.Platform.TrySetLargeIconResource(notification.Android.LargeIconResourceName, builder);

            if (!notification.Android.ColorResourceName.IsEmpty())
            {
                var color = this.Services.Platform.GetColorResourceId(notification.Android.ColorResourceName!);
                builder.SetColor(color);
            }

            if (notification.Android.ShowWhen != null)
                builder.SetShowWhen(notification.Android.ShowWhen.Value);

            if (notification.Android.When != null)
                builder.SetWhen(notification.Android.When.Value.ToUnixTimeMilliseconds());

            this.ApplyChannel(builder, notification, channel);
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
            => this.Services.Platform.GetBroadcastPendingIntent<ShinyNotificationBroadcastReceiver>(
                ShinyNotificationBroadcastReceiver.AlarmIntentAction,
                PendingIntentFlags.UpdateCurrent,
                0,
                intent => intent.PutExtra(AndroidNotificationProcessor.IntentNotificationKey, notification.Id)
            );


        AlarmManager? alarms;
        public AlarmManager Alarms => this.alarms ??= this.Services.Platform.GetSystemService<AlarmManager>(Context.AlarmService);


        public virtual void ApplyLaunchIntent(NotificationCompat.Builder builder, Notification notification)
        {
            var pendingIntent = this.GetLaunchPendingIntent(notification);
            builder.SetContentIntent(pendingIntent);
        }


        public virtual void ApplyChannel(NotificationCompat.Builder builder, Notification notification, Channel channel)
        {
            if (channel == null)
                return;

            builder.SetChannelId(channel.Identifier);
            if (channel.Actions != null)
            {
                foreach (var action in channel.Actions)
                {
                    switch (action.ActionType)
                    {
                        case ChannelActionType.OpenApp:
                            break;

                        case ChannelActionType.TextReply:
                            var textReplyAction = this.CreateTextReply(notification, action);
                            builder.AddAction(textReplyAction);
                            break;

                        case ChannelActionType.None:
                        case ChannelActionType.Destructive:
                            var destAction = this.CreateAction(notification, action);
                            builder.AddAction(destAction);
                            break;

                        default:
                            throw new ArgumentException("Invalid action type");
                    }
                }
            }
        }


        public virtual PendingIntent GetLaunchPendingIntent(Notification notification, string? actionId = null)
        {
            Intent launchIntent;

            if (notification.Android.LaunchActivityType == null)
            {
                launchIntent = this.Services!
                    .Platform!
                    .AppContext!
                    .PackageManager!
                    .GetLaunchIntentForPackage(this.Services!.Platform!.Package!.PackageName!)!
                    .SetFlags(notification.Android.LaunchActivityFlags.ToNative());
            }
            else
            {
                launchIntent = new Intent(
                    this.Services.Platform.AppContext,
                    notification.Android.LaunchActivityType
                );
            }

            this.PopulateIntent(launchIntent, notification);

            PendingIntent pendingIntent;
            if ((notification.Android.LaunchActivityFlags & AndroidActivityFlags.ClearTask) != 0)
            {
                pendingIntent = AndroidX.Core.App.TaskStackBuilder
                    .Create(this.Services.Platform.AppContext)
                    .AddNextIntent(launchIntent)
                    .GetPendingIntent(
                        notification.Id,
                        (int)this.Services.Platform.GetPendingIntentFlags(PendingIntentFlags.OneShot)
                    );
            }
            else
            {
                pendingIntent = PendingIntent.GetActivity(
                    this.Services.Platform.AppContext!,
                    notification.Id,
                    launchIntent!,
                    this.Services.Platform.GetPendingIntentFlags(PendingIntentFlags.OneShot)
                )!;
            }
            return pendingIntent;
        }



        static int counter = 100;
        protected virtual PendingIntent CreateActionIntent(Notification notification, ChannelAction action)
        {
            counter++;
            return this.Services.Platform.GetBroadcastPendingIntent<ShinyNotificationBroadcastReceiver>(
                ShinyNotificationBroadcastReceiver.EntryIntentAction,
                PendingIntentFlags.UpdateCurrent,
                counter,
                intent =>
                {
                    this.PopulateIntent(intent, notification);
                    intent.PutExtra(AndroidNotificationProcessor.IntentActionKey, action.Identifier);
                }
            );
        }


        protected virtual void PopulateIntent(Intent intent, Notification notification)
        {
            var content = this.Services.Serializer.Serialize(notification);
            intent.PutExtra(AndroidNotificationProcessor.IntentNotificationKey, content);
        }


        protected virtual NotificationCompat.Action CreateAction(Notification notification, ChannelAction action)
        {
            var pendingIntent = this.CreateActionIntent(notification, action);
            var iconId = this.Services.Platform.GetResourceIdByName(action.Identifier);
            var nativeAction = new NotificationCompat.Action.Builder(iconId, action.Title, pendingIntent).Build();

            return nativeAction;
        }


        protected virtual NotificationCompat.Action CreateTextReply(Notification notification, ChannelAction action)
        {
            var pendingIntent = this.CreateActionIntent(notification, action);
            var input = new AndroidX.Core.App.RemoteInput.Builder(AndroidNotificationProcessor.RemoteInputResultKey)
                .SetLabel(action.Title)
                .Build();

            var iconId = this.Services.Platform.GetResourceIdByName(action.Identifier);
            var nativeAction = new NotificationCompat.Action.Builder(iconId, action.Title, pendingIntent)
                .SetAllowGeneratedReplies(true)
                .AddRemoteInput(input)
                .Build();

            return nativeAction;
        }


        public virtual void SendNative(int id, Android.App.Notification notification)
            => this.NativeManager.Notify(id, notification);
    }
}
