using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.ApplicationModel.Background;
using Shiny.Jobs;
using Shiny.Settings;
using Shiny.Infrastructure;


namespace Shiny.Notifications
{
    //https://blogs.msdn.microsoft.com/tiles_and_toasts/2015/07/08/quickstart-sending-a-local-toast-notification-and-handling-activations-from-it-windows-10/
    public class NotificationManager : INotificationManager, IShinyStartupTask
    {
        readonly ToastNotifier toastNotifier;
        readonly IServiceProvider services;
        readonly IRepository repository;
        readonly IJobManager jobs;
        readonly ISettings settings;
        readonly UwpContext context;
        readonly BadgeUpdater badgeUpdater;

        public NotificationManager(IServiceProvider services,
                                   IJobManager jobs,
                                   ISettings settings,
                                   IRepository repository,
                                   UwpContext context)
        {
            this.toastNotifier = ToastNotificationManager.CreateToastNotifier();
            this.badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();
            this.services = services;
            this.jobs = jobs;
            this.settings = settings;
            this.repository = repository;
            this.context = context;
        }


        public Task<AccessState> RequestAccess()
            => this.jobs.RequestAccess();


        public async Task Send(Notification notification)
        {
            if (notification.Id == 0)
                notification.Id = this.settings.IncrementValue("NotificationId");

            if (notification.ScheduleDate != null)
            {
                await this.repository.Set(notification.Id.ToString(), notification);
                return;
            }

            var toastContent = new ToastContent
            {
                Duration = notification.Windows.UseLongDuration ? ToastDuration.Long : ToastDuration.Short,
                Launch = notification.Payload,
                ActivationType = ToastActivationType.Background,
                Visual = new ToastVisual
                {
                    BindingGeneric = new ToastBindingGeneric
                    {
                        Children =
                        {
                            new AdaptiveText
                            {
                                Text = notification.Title
                            },
                            new AdaptiveText
                            {
                                Text = notification.Message
                            }
                        }
                    }
                }
            };

            if (!Notification.CustomSoundFilePath.IsEmpty())
                toastContent.Audio = new ToastAudio { Src = new Uri(Notification.CustomSoundFilePath) };

            this.toastNotifier.Show(new ToastNotification(toastContent.GetXml()));
            if (notification.BadgeCount != null)
                await this.SetBadge(notification.BadgeCount.Value);

            await this.services.SafeResolveAndExecute<INotificationDelegate>(x => x.OnReceived(notification));
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


        public Task Clear() => this.repository.Clear<Notification>();
        public async Task<IEnumerable<Notification>> GetPending() => await this.repository.GetAll<Notification>();
        public Task Cancel(int id) => this.repository.Remove<Notification>(id.ToString());
        public void Start()
        {
            if (this.services.IsRegistered<INotificationDelegate>())
            {
                this.context.RegisterBackground<NotificationBackgroundTaskProcessor>(
                    nameof(NotificationBackgroundTaskProcessor),
                    builder => builder.SetTrigger(new UserNotificationChangedTrigger(NotificationKinds.Toast))
                );
            }
        }


        const string BADGE_KEY = "ShinyNotificationBadge";
        public Task SetBadge(int value)
        {
            var badge = new BadgeNumericContent((uint)value);
            this.badgeUpdater.Update(new BadgeNotification(badge.GetXml()));
            this.settings.Set(BADGE_KEY, value);
            return Task.CompletedTask;
        }


        public Task<int> GetBadge()
        {
            var badge = this.settings.Get(BADGE_KEY, 0);
            return Task.FromResult(badge);
        }
    }
}