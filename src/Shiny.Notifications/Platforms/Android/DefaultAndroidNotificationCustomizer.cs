using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using AndroidX.Core.App;
using Shiny.Infrastructure;
using Native = AndroidX.Core.App.NotificationCompat.Builder;
using TaskStackBuilder = AndroidX.Core.App.TaskStackBuilder;

namespace Shiny.Notifications;


public class DefaultAndroidNotificationCustomizer : INotificationCustomizer
{
    readonly AndroidPlatform platform;
    readonly ISerializer serializer;
    readonly AndroidCustomizationOptions options;


    public DefaultAndroidNotificationCustomizer(
        AndroidPlatform platform,
        ISerializer serializer,
        AndroidCustomizationOptions? options = null
    )
    {
        this.platform = platform;
        this.serializer = serializer;
        this.options = options ?? new AndroidCustomizationOptions();
    }


    public async Task Customize(Notification notification, Channel channel, Native builder)
    {
        this.ApplyChannel(builder, notification, channel);

        builder
            .SetContentTitle(notification.Title)
            .SetContentIntent(this.GetLaunchPendingIntent(notification))
            .SetSmallIcon(this.platform.GetSmallIconResource(this.options.SmallIconResourceName))
            .SetAutoCancel(this.options.AutoCancel)
            .SetOngoing(this.options.OnGoing);

        if (!notification.Thread.IsEmpty())
            builder.SetGroup(notification.Thread);

        if (!notification.LocalAttachmentPath.IsEmpty())
            this.platform.TrySetImage(notification.LocalAttachmentPath, builder);

        //if (notification.BadgeCount != null)
        //{
        //    // channel needs badge too
        //    builder
        //        .SetBadgeIconType(NotificationCompat.BadgeIconSmall)
        //        .SetNumber(notification.BadgeCount.Value);
        //}

        if (!this.options.Ticker.IsEmpty())
            builder.SetTicker(this.options.Ticker);

        if (this.options.UseBigTextStyle)
            builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(notification.Message));
        else
            builder.SetContentText(notification.Message);

        if (!this.options.LargeIconResourceName.IsEmpty())
        {
            var iconId = this.platform.GetResourceIdByName(this.options.LargeIconResourceName!);
            if (iconId > 0)
                builder.SetLargeIcon(BitmapFactory.DecodeResource(this.platform.AppContext.Resources, iconId));
        }

        if (!this.options.ColorResourceName.IsEmpty())
        {
            var color = this.platform.GetColorResourceId(this.options.ColorResourceName!);
            builder.SetColor(color);
        }
    }


    public virtual PendingIntent GetLaunchPendingIntent(Notification notification)
    {
        Intent launchIntent;

        if (this.options.LaunchActivityType == null)
        {
            launchIntent = this.platform!
                .AppContext!
                .PackageManager!
                .GetLaunchIntentForPackage(this.platform!.Package!.PackageName!)!
                .SetFlags(this.options.LaunchActivityFlags);
        }
        else
        {
            launchIntent = new Intent(
                this.platform.AppContext,
                this.options.LaunchActivityType
            );
        }

        this.PopulateIntent(launchIntent, notification);

        PendingIntent pendingIntent;
        if ((this.options.LaunchActivityFlags & ActivityFlags.ClearTask) != 0)
        {
            pendingIntent = TaskStackBuilder
                .Create(this.platform.AppContext)
                .AddNextIntent(launchIntent)
                .GetPendingIntent(
                    notification.Id,
                    (int)this.platform.GetPendingIntentFlags(PendingIntentFlags.OneShot)
                )!;
        }
        else
        {
            pendingIntent = PendingIntent.GetActivity(
                this.platform.AppContext!,
                notification.Id,
                launchIntent!,
                this.platform.GetPendingIntentFlags(PendingIntentFlags.OneShot)
            )!;
        }
        return pendingIntent;
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


    protected virtual void PopulateIntent(Intent intent, Notification notification)
    {
        var content = this.serializer.Serialize(notification);
        intent.PutExtra(AndroidNotificationProcessor.IntentNotificationKey, content);
    }


    static int counter = 100;
    protected virtual PendingIntent CreateActionIntent(Notification notification, ChannelAction action)
    {
        counter++;
        return this.platform.GetBroadcastPendingIntent<ShinyNotificationBroadcastReceiver>(
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


    protected virtual NotificationCompat.Action CreateAction(Notification notification, ChannelAction action)
    {
        var pendingIntent = this.CreateActionIntent(notification, action);
        var iconId = this.platform.GetResourceIdByName(action.Identifier);
        var nativeAction = new NotificationCompat.Action.Builder(iconId, action.Title, pendingIntent).Build();

        return nativeAction;
    }


    protected virtual NotificationCompat.Action CreateTextReply(Notification notification, ChannelAction action)
    {
        var pendingIntent = this.CreateActionIntent(notification, action);
        var input = new AndroidX.Core.App.RemoteInput.Builder(AndroidNotificationProcessor.RemoteInputResultKey)
            .SetLabel(action.Title)
            .Build();

        var iconId = this.platform.GetResourceIdByName(action.Identifier);
        var nativeAction = new NotificationCompat.Action.Builder(iconId, action.Title, pendingIntent)
            .SetAllowGeneratedReplies(true)
            .AddRemoteInput(input)
            .Build();

        return nativeAction;
    }
}