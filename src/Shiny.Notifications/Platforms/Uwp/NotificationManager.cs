using System;
using System.Linq;
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
    public class NotificationManager : INotificationManager, IPersistentNotificationManagerExtension
    {
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
            this.badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();
            this.services = services;
            this.jobs = jobs;
            this.settings = settings;
            this.repository = repository;
        }


        public IPersistentNotification Create(Notification notification)
        {
            //AdaptiveProgressBarValue.Indeterminate;
            var native = this.CreateNativeNotification(notification, true);
            notification.ScheduleDate = null;
            var pnotification = new UwpPersistentNotification(notification);
            ToastNotificationManager.CreateToastNotifier().Show(native);

            return pnotification;
        }


        public Task<AccessState> RequestAccess()
            => this.jobs.RequestAccess();


        public async Task Send(Notification notification)
        {
            var native = this.CreateNativeNotification(notification, false);
            if (notification.ScheduleDate != null)
            {
                await this.repository.Set(notification.Id.ToString(), notification);
                return;
            }

            ToastNotificationManager.CreateToastNotifier().Show(native);
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

        public async Task<IEnumerable<Notification>> GetPending() => await this.repository.GetAll<Notification>();


        public async Task Clear()
        {
            ToastNotificationManager.History.Clear();
            await this.repository.Clear<Notification>();
        }


        public async Task Cancel(int id)
        {
            ToastNotificationManager.History.Remove(id.ToString());
            await this.repository.Remove<Notification>(id.ToString());
        }


        //public void Start()
        //{
        //    //if (this.services.IsRegistered<INotificationDelegate>())
        //    //{
        //    //    UwpShinyHost.RegisterBackground<NotificationBackgroundTaskProcessor>(
        //    //        builder => builder.SetTrigger(new UserNotificationChangedTrigger(NotificationKinds.Toast))
        //    //    );
        //    //}
        //}


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


        public ToastNotification CreateNativeNotification(Notification notification, bool includeProgressBar)
        {
            if (notification.Id == 0)
                notification.Id = this.settings.IncrementValue("NotificationId");

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

                            //new AdaptiveProgressBar()
                            //{
                            //    Title = "Weekly playlist",
                            //    Value = new BindableProgressBarValue("progressValue"),
                            //    ValueStringOverride = new BindableString("progressValueString"),
                            //    Status = new BindableString("progressStatus")
                            //}
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

            var native = new ToastNotification(toastContent.GetXml())
            {
                Tag = notification.Id.ToString(),
                Group = notification.Windows.GroupName
            };
            return native;
        }
    }
}