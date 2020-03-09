using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.ApplicationModel.Background;
using Shiny.Jobs;
using Shiny.Settings;
using Shiny.Infrastructure;
using System.Linq;

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
        readonly BadgeUpdater badgeUpdater;

        public NotificationManager(IServiceProvider services,
                                   IJobManager jobs,
                                   ISettings settings,
                                   IRepository repository)
        {
            this.toastNotifier = ToastNotificationManager.CreateToastNotifier();
            this.badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();
            this.services = services;
            this.jobs = jobs;
            this.settings = settings;
            this.repository = repository;
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
                //Launch = notification.Payload,
                ActivationType = ToastActivationType.Background,
                //ActivationType = ToastActivationType.Foreground,
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
            if (!notification.Category.IsEmpty())
            {
                var category = this.registeredCategories.FirstOrDefault(x => x.Identifier.Equals(notification.Category));

                var nativeActions = new ToastActionsCustom();

                foreach (var action in category.Actions)
                {
                    switch (action.ActionType)
                    {
                        case NotificationActionType.OpenApp:
                            nativeActions.Buttons.Add(new ToastButton(action.Title, action.Identifier)
                            {
                                //ActivationType = ToastActivationType.Foreground
                                ActivationType = ToastActivationType.Background
                            });
                            break;

                        case NotificationActionType.None:
                        case NotificationActionType.Destructive:
                            nativeActions.Buttons.Add(new ToastButton(action.Title, action.Identifier)
                            {
                                ActivationType = ToastActivationType.Background
                            });
                            break;

                        case NotificationActionType.TextReply:
                            nativeActions.Inputs.Add(new ToastTextBox(action.Identifier)
                            {
                                Title = notification.Title
                                //DefaultInput = "",
                                //PlaceholderContent = ""
                            });
                            break;
                    }
                }
                toastContent.Actions = nativeActions;
            }

            //if (!Notification.CustomSoundFilePath.IsEmpty())
            //    toastContent.Audio = new ToastAudio { Src = new Uri(Notification.CustomSoundFilePath) };

            var native = new ToastNotification(toastContent.GetXml());
            this.toastNotifier.Show(native);
            if (notification.BadgeCount != null)
                this.Badge = notification.BadgeCount.Value;

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


        readonly List<NotificationCategory> registeredCategories = new List<NotificationCategory>();
        public void RegisterCategory(NotificationCategory category) => this.registeredCategories.Add(category);

        public Task Clear() => this.repository.Clear<Notification>();
        public async Task<IEnumerable<Notification>> GetPending() => await this.repository.GetAll<Notification>();
        public Task Cancel(int id) => this.repository.Remove<Notification>(id.ToString());
        public void Start()
        {
            if (this.services.IsRegistered<INotificationDelegate>())
            {
                //this.context.RegisterBackground<NotificationBackgroundTaskProcessor>(
                //    nameof(NotificationBackgroundTaskProcessor),
                //    builder => builder.SetTrigger(new UserNotificationChangedTrigger(NotificationKinds.Toast))
                //);
            }
        }


        const string BADGE_KEY = "ShinyNotificationBadge";
        public int Badge
        {
            get => this.settings.Get(BADGE_KEY, 0);
            set
            {
                var badge = new BadgeNumericContent((uint)value);
                this.badgeUpdater.Update(new BadgeNotification(badge.GetXml()));
                this.settings.Set(BADGE_KEY, value);
            }
        }
    }
}