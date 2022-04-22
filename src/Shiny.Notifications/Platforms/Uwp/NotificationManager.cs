using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using Windows.ApplicationModel.Background;
using Shiny.Infrastructure;
using Shiny.Jobs;


namespace Shiny.Notifications
{
    public class NotificationManager : INotificationManager, IShinyStartupTask
    {
        readonly ShinyCoreServices services;
        readonly IJobManager jobManager;
        readonly BadgeUpdater badgeUpdater;


        public NotificationManager(ShinyCoreServices services, IJobManager jobManager)
        {
            this.badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();
            this.services = services;
            this.jobManager = jobManager;
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


        public Task<AccessState> RequestAccess(AccessRequestFlags access)
            => this.jobManager.RequestAccess();


        public async Task Send(Notification notification)
        {
            // create the notification to validate it
            if (notification.Id == 0)
                notification.Id = this.services.Settings.IncrementValue("NotificationId");

            var builder = new ToastContentBuilder()
                .AddText(notification.Title, AdaptiveTextStyle.Title)
                .AddText(notification.Message, AdaptiveTextStyle.Subtitle)
                .SetToastDuration(ToastDuration.Short)
                .AddToastActivationInfo(notification.Id.ToString(), ToastActivationType.Foreground);
            await this.TrySetChannel(notification, builder);

            if (notification.ScheduleDate != null)
            {
                await this.services.Repository.Set(notification.Id.ToString(), notification);
                // TODO: set badge and fire notification fired?
                builder.Schedule(notification.ScheduleDate.Value);
            }
            else
            {
                if (notification.BadgeCount != null)
                    await this.SetBadge(notification.BadgeCount.Value);

                builder.Show(new CustomizeToast(x =>
                {
                    //x.Activated += null;
                    //x.Dismissed += null;
                    x.Tag = notification.Id.ToString();
                    //x.Group = "";
                    //x.Priority
                }));
            }
        }


        public async Task<IEnumerable<Notification>> GetPendingNotifications()
            => await this.services.Repository.GetList<Notification>();

        public Task<Notification?> GetNotification(int notificationId)
            => this.services.Repository.Get<Notification>(notificationId.ToString());


        public async Task Cancel(CancelScope scope)
        {
            if (scope == CancelScope.DisplayedOnly || scope == CancelScope.All)
                ToastNotificationManagerCompat.History.Clear();

            if (scope == CancelScope.Pending || scope == CancelScope.All)
                await this.services.Repository.Clear<Notification>();
        }


        public async Task Cancel(int id)
        {
            ToastNotificationManagerCompat.History.Remove(id.ToString());
            await this.services.Repository.Remove<Notification>(id.ToString());
        }


        const string BADGE_KEY = "ShinyNotificationBadge";
        public Task<int> GetBadge() => Task.FromResult(this.services.Settings.Get<int>(BADGE_KEY));
        public Task SetBadge(int? badge)
        {
            var value = badge ?? 0;
            var badgeContent = new BadgeNumericContent((uint)value);
            this.badgeUpdater.Update(new BadgeNotification(badgeContent.GetXml()));
            this.services.Settings.Set(BADGE_KEY, value);
            return Task.CompletedTask;
        }


        public Task<Channel?> GetChannel(string identifier) => this.services.Repository.Get<Channel>(identifier);
        public Task<IList<Channel>> GetChannels() => this.services.Repository.GetList<Channel>();
        public Task AddChannel(Channel channel) => this.services.Repository.Set(channel.Identifier, channel);
        public Task RemoveChannel(string channelId) => this.services.Repository.Remove<Channel>(channelId);
        public Task ClearChannels() => this.services.Repository.Clear<Channel>();


        protected async Task TrySetChannel(Notification notification, ToastContentBuilder builder)
        {
            Channel? channel = null;
            if (!notification.Channel.IsEmpty())
                channel = await this.GetChannel(notification.Channel!);

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
                                .SetProtocolActivation(new Uri("empty"))
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
                            builder
                                .AddArgument(action.Identifier)
                                .AddInputTextBox("Text", null, action.Title)
                                .AddButton(new ToastButton()
                                    .SetContent("OK")
                                    .SetBackgroundActivation()
                                )
                                .AddButton(new ToastButton()
                                    .SetContent("Cancel")
                                    .SetDismissActivation()
                                );
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