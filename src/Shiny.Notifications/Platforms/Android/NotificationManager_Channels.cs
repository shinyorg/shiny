using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Media;
using AndroidX.Core.App;


namespace Shiny.Notifications
{
    public partial class NotificationManager
    {
        public async Task SetChannels(params Channel[] channels)
        {
            var existing = await this.core.Repository.GetChannels();
            foreach (var exist in existing)
                this.manager.NativeManager.DeleteNotificationChannel(exist.Identifier);

            await this.core.Repository.DeleteAllChannels();
            foreach (var channel in channels)
                await this.CreateChannel(channel);
        }


        public Task<IList<Channel>> GetChannels()
            => this.core.Repository.GetChannels();


        protected async Task<Channel> GetChannel(Notification notification)
        {
            //if (this.manager.GetNotificationChannel(Channel.Default.Identifier) == null)
            //    this.CreateNativeChannel(Channel.Default);
            //var channel = Channel.Default;
            //if (notification.Channel.IsEmpty())
            //{
            //    if (this.manager.GetNotificationChannel(Channel.Default.Identifier) == null)
            //        this.CreateNativeChannel(Channel.Default);
            //}
            //else
            //{
            //    channel = await this.core.Repository.GetChannel(notification.Channel);
            //    if (channel == null)
            //        throw new ArgumentException($"{notification.Channel} does not exist");
            //}
            var channel = await this.core.Repository.Get<Channel>(notification.Channel ?? Channel.Default.Identifier);
            return channel;
        }

        protected virtual void ApplyChannel(Notification notification, Channel channel, NotificationCompat.Builder builder)
        {
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


        static int counter = 100;
        protected virtual PendingIntent CreateActionIntent(Notification notification, ChannelAction action)
        {
            var intent = this.core.Android.CreateIntent<ShinyNotificationBroadcastReceiver>(ShinyNotificationBroadcastReceiver.EntryIntentAction);
            var content = this.core.Serializer.Serialize(notification);
            intent
                .PutExtra("Notification", content)
                .PutExtra("Action", action.Identifier);

            counter++;
            var pendingIntent = PendingIntent.GetBroadcast(
                this.core.Android.AppContext,
                counter,
                intent,
                PendingIntentFlags.UpdateCurrent
            )!;
            return pendingIntent;
        }


        protected virtual NotificationCompat.Action CreateAction(Notification notification, ChannelAction action)
        {
            var pendingIntent = this.CreateActionIntent(notification, action);
            var iconId = this.core.Android.GetResourceIdByName(action.Identifier);
            var nativeAction = new NotificationCompat.Action.Builder(iconId, action.Title, pendingIntent).Build();

            return nativeAction;
        }


        protected virtual NotificationCompat.Action CreateTextReply(Notification notification, ChannelAction action)
        {
            var pendingIntent = this.CreateActionIntent(notification, action);
            var input = new AndroidX.Core.App.RemoteInput.Builder("Result")
                .SetLabel(action.Title)
                .Build();

            var iconId = this.core.Android.GetResourceIdByName(action.Identifier);
            var nativeAction = new NotificationCompat.Action.Builder(iconId, action.Title, pendingIntent)
                .SetAllowGeneratedReplies(true)
                .AddRemoteInput(input)
                .Build();

            return nativeAction;
        }


        protected virtual void CreateNativeChannel(Channel channel)
        {
            var native = new NotificationChannel(
                channel.Identifier,
                channel.Description ?? channel.Identifier,
                channel.Importance.ToNative()
            );
            var attrBuilder = new AudioAttributes.Builder();

            Android.Net.Uri uri = null;
            if (!channel.CustomSoundPath.IsEmpty())

            switch (channel.Importance)
            {
                case ChannelImportance.Critical:
                    attrBuilder
                        .SetUsage(AudioUsageKind.Alarm)
                        .SetFlags(AudioFlags.AudibilityEnforced);

                    uri ??= Android.Provider.Settings.System.DefaultAlarmAlertUri;
                    break;

                case ChannelImportance.High:
                    uri ??= Android.Provider.Settings.System.DefaultAlarmAlertUri;
                    break;

                case ChannelImportance.Normal:
                    uri ??= Android.Provider.Settings.System.DefaultNotificationUri;
                    break;

                case ChannelImportance.Low:
                    break;
            }
            if (uri != null)
                native.SetSound(uri, attrBuilder.Build());

            this.manager.NativeManager.CreateNotificationChannel(native);
        }


        protected virtual async Task CreateChannel(Channel channel)
        {
            channel.AssertValid();
            this.CreateNativeChannel(channel);

            await this.core.Repository.SetChannel(channel);
        }
    }
}
