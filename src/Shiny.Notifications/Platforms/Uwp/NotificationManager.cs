using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using Windows.ApplicationModel.Background;
using Shiny.Infrastructure;


namespace Shiny.Notifications
{
    public class NotificationManager : INotificationManager, IShinyStartupTask
    {
        readonly ShinyCoreServices services;
        readonly BadgeUpdater badgeUpdater;


        public NotificationManager(ShinyCoreServices services)
        {
            this.badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();
            this.services = services;
        }


        public void Start()
        {
            if (this.services.Services.GetService(typeof(INotificationDelegate)) != null)
            {
                //ToastNotificationManagerCompat.OnActivated += null;
                UwpPlatform.RegisterBackground<NotificationBackgroundTaskProcessor>(
                    builder => builder.SetTrigger(new ToastNotificationActionTrigger())
                );
            }
        }


        public Task<AccessState> RequestAccess()
            => this.services.Jobs.RequestAccess();


        public async Task Send(Notification notification)
        {
            // create the notification to validate it
            if (notification.Id == 0)
                notification.Id = this.services.Settings.IncrementValue("NotificationId");


            if (notification.ScheduleDate != null && notification.ScheduleDate > DateTimeOffset.UtcNow)
            {
                await this.services.Repository.Set(notification.Id.ToString(), notification);
                return;
            }
            if (notification.BadgeCount != null)
                this.Badge = notification.BadgeCount.Value;

            var custom = new CustomizeToast(x =>
            {
                //x.Activated += null;
                //x.Dismissed += null;
                x.Tag = notification.Id.ToString();
                x.Group = "";
                //x.Priority
            });
            var builder = new ToastContentBuilder()
                .AddText(notification.Title, AdaptiveTextStyle.Title)
                .AddText(notification.Message, AdaptiveTextStyle.Subtitle)
                .SetToastDuration(ToastDuration.Short)
                .AddToastActivationInfo(notification.Id.ToString(), ToastActivationType.Foreground);

            await this.TrySetChannel(notification, builder);

            await this.services
                .Services
                .SafeResolveAndExecute<INotificationDelegate>(
                    x => x.OnReceived(notification)
                );
        }


        public async Task<IEnumerable<Notification>> GetPending()
            => await this.services.Repository.GetAll<Notification>();


        public async Task Clear()
        {
            ToastNotificationManager.History.Clear();
            await this.services.Repository.Clear<Notification>();
        }


        public async Task Cancel(int id)
        {
            ToastNotificationManager.History.Remove(id.ToString());
            await this.services.Repository.Remove<Notification>(id.ToString());
        }


        const string BADGE_KEY = "ShinyNotificationBadge";
        public int Badge
        {
            get => this.services.Settings.Get<int>(BADGE_KEY);
            set
            {
                var badge = new BadgeNumericContent((uint)value);
                this.badgeUpdater.Update(new BadgeNotification(badge.GetXml()));
                this.services.Settings.Set(BADGE_KEY, value);
            }
        }


        public async Task SetChannels(params Channel[] channels)
        {
            await this.services.Repository.DeleteAllChannels();
            foreach (var channel in channels)
                await this.services.Repository.SetChannel(channel);
        }


        public Task<IList<Channel>> GetChannels() => this.services.Repository.GetChannels();


        protected async Task TrySetChannel(Notification notification, ToastContentBuilder builder)
        {
            Channel? channel = null;
            if (!notification.Channel.IsEmpty())
                channel = await this.services.Repository.GetChannel(notification.Channel);

            channel ??= Channel.Default;

            //if (!channel.CustomSoundPath.IsEmpty())
            //    builder.AddAudio(new Uri(channel.CustomSoundPath));

            if (channel.Actions.Any())
            {
                foreach (var action in channel.Actions)
                {
                    switch (action.ActionType)
                    {
                        case ChannelActionType.OpenApp:
                            // foreground activation
                            builder.AddButton(new ToastButton()
                                .SetContent(action.Title)
                                .AddArgument("action", action.Identifier)
                            );
                            break;

                        case ChannelActionType.None:
                        case ChannelActionType.Destructive:
                            builder.AddButton(new ToastButton()
                                .SetBackgroundActivation()
                                .SetContent(action.Title)
                                .AddArgument("action", action.Identifier)
                            );
                            break;

                        case ChannelActionType.TextReply:
                            builder.AddInputTextBox(action.Identifier, null, action.Title);
                            // TODO: need  button?
                            break;
                    }
                }
            }
        }


        //string BuildSoundPath(string sound)
        //{
        //    var ext = Path.GetExtension(sound);
        //    if (String.IsNullOrWhiteSpace(ext))
        //        sound += ".mp4";

        //    if (sound.StartsWith("ms-appx://"))
        //        sound = "ms-appx://" + sound;

        //    return sound;
        //}
    }
}