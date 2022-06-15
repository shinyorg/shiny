using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using AndroidX.Core.App;
using Shiny.Infrastructure;
using Native = AndroidX.Core.App.NotificationCompat.Builder;

namespace Shiny.Notifications;


public class DefaultAndroidNotificationCustomizer : INotificationCustomizer
{
    readonly AndroidPlatform platform;
    readonly ISerializer serializer;
    readonly AndroidCustomizationOptions? options;
    readonly Action<Notification, Channel, Native>? onCustomize;


    public DefaultAndroidNotificationCustomizer(
        AndroidPlatform platform, 
        ISerializer serializer,
        AndroidCustomizationOptions? options, 
        Action<Notification, Channel, Native>? onCustomize
    )
    {
        this.platform = platform;
        this.serializer = serializer;
        this.options = options ?? new AndroidCustomizationOptions();
        this.onCustomize = onCustomize;
    }


    public Task Customize(Notification notification, Channel channel, Native builder)
    {
        this.ApplyChannel(builder, notification, channel);

        builder
            .SetContentTitle(notification.Title)
            .SetSmallIcon(this.platform.GetSmallIconResource(this.options.SmallIconResourceName))
            .SetAutoCancel(this.options.AutoCancel)
            .SetOngoing(this.options.OnGoing)
            .SetContentIntent(this.GetLaunchPendingIntent(notification));

        
        //if (!notification.Thread.IsEmpty())
        //    builder.SetGroup(notification.Thread);

        //this.ApplyLaunchIntent(builder, notification);
        //if (!this.options.ContentInfo.IsEmpty())
        //    builder.SetContentInfo(this.options.ContentInfo);

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

        //this.platform.TrySetLargeIconResource(notification.Android.LargeIconResourceName, builder);

        if (!this.options.ColorResourceName.IsEmpty())
        {
            var color = this.platform.GetColorResourceId(this.options.ColorResourceName!);
            builder.SetColor(color);
        }
        

        this.onCustomize?.Invoke(notification, channel, builder);

        return Task.CompletedTask;
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

        // TODO
        //var content = this.serializer.Serialize(notification);
        //intent.PutExtra(AndroidNotificationProcessor.IntentNotificationKey, content);
        //this.PopulateIntent(launchIntent, notification);

        PendingIntent pendingIntent;
        if ((this.options.LaunchActivityFlags & ActivityFlags.ClearTask) != 0)
        {
            pendingIntent = AndroidX.Core.App.TaskStackBuilder
                .Create(this.platform.AppContext)
                .AddNextIntent(launchIntent)
                .GetPendingIntent(
                    notification.Id,
                    (int)this.platform.GetPendingIntentFlags(PendingIntentFlags.OneShot)
                );
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