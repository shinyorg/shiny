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
                UwpPlatform.RegisterBackground<NotificationBackgroundTaskProcessor>(
                    builder => builder.SetTrigger(new ToastNotificationActionTrigger())
                );
            }
        }


        //https://docs.microsoft.com/en-us/windows/uwp/design/shell/tiles-and-notifications/notification-listener


        //IReadOnlyList<UserNotification> notifs = await listener.GetNotificationsAsync(NotificationKinds.Toast);


        //        // Get the listener
        //        UserNotificationListener listener = UserNotificationListener.Current;

        //        // And request access to the user's notifications (must be called from UI thread)
        //        UserNotificationListenerAccessStatus accessStatus = await listener.RequestAccessAsync();

        //switch (accessStatus)
        //{
        //    // This means the user has granted access.
        //    case UserNotificationListenerAccessStatus.Allowed:

        //        // Yay! Proceed as normal
        //        break;

        //    // This means the user has denied access.
        //    // Any further calls to RequestAccessAsync will instantly
        //    // return Denied. The user must go to the Windows settings
        //    // and manually allow access.
        //    case UserNotificationListenerAccessStatus.Denied:

        //        // Show UI explaining that listener features will not
        //        // work until user allows access.
        //        break;

        //    // This means the user closed the prompt without
        //    // selecting either allow or deny. Further calls to
        //    // RequestAccessAsync will show the dialog again.
        //    case UserNotificationListenerAccessStatus.Unspecified:

        //        // Show UI that allows the user to bring up the prompt again
        //        break;
        //}
        public Task<AccessState> RequestAccess()
            => this.services.Jobs.RequestAccess();


        public async Task Send(Notification notification)
        {
            // create the notification to validate it
            if (notification.Id == 0)
                notification.Id = this.services.Settings.IncrementValue("NotificationId");

            var content = await this.CreateContent(notification);
            var native = this.CreateNativeNotification(content, notification);

            if (notification.ScheduleDate != null && notification.ScheduleDate > DateTimeOffset.UtcNow)
            {
                await this.services.Repository.Set(notification.Id.ToString(), notification);
                return;
            }

            ToastNotificationManager.CreateToastNotifier().Show(native);
            if (notification.BadgeCount != null)
                this.Badge = notification.BadgeCount.Value;

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
            get => this.services.Settings.Get(BADGE_KEY, 0);
            set
            {
                var badge = new BadgeNumericContent((uint)value);
                this.badgeUpdater.Update(new BadgeNotification(badge.GetXml()));
                this.services.Settings.Set(BADGE_KEY, value);
            }
        }


        protected virtual ToastNotification CreateNativeNotification(ToastContent toastContent, Notification notification)
            => new ToastNotification(toastContent.GetXml())
            {
                Tag = notification.Id.ToString(),
                Group = notification.Channel ?? Channel.Default.Identifier
            };


        public async Task SetChannels(params Channel[] channels)
        {
            await this.services.Repository.DeleteAllChannels();
            foreach (var channel in channels)
                await this.services.Repository.SetChannel(channel);
        }


        public Task<IList<Channel>> GetChannels() => this.services.Repository.GetChannels();


        protected async Task TrySetChannel(Notification notification, ToastContent content)
        {
            Channel? channel = null;
            if (!notification.Channel.IsEmpty())
                channel = await this.services.Repository.GetChannel(notification.Channel);

            channel ??= Channel.Default;

            //if (!Notification.CustomSoundFilePath.IsEmpty())
            //    toastContent.Audio = new ToastAudio { Src = new Uri(channel.CustomSoundPath) };

            if (channel.Actions.Any())
            {
                var nativeActions = new ToastActionsCustom();

                foreach (var action in channel.Actions)
                {
                    switch (action.ActionType)
                    {
                        case ChannelActionType.OpenApp:
                            nativeActions.Buttons.Add(new ToastButton(action.Title, action.Identifier)
                            {
                                ActivationType = ToastActivationType.Foreground
                            });
                            break;

                        case ChannelActionType.None:
                        case ChannelActionType.Destructive:
                            nativeActions.Buttons.Add(new ToastButton(action.Title, action.Identifier)
                            {
                                ActivationType = ToastActivationType.Background
                            });
                            break;

                        case ChannelActionType.TextReply:
                            nativeActions.Inputs.Add(new ToastTextBox(action.Identifier)
                            {
                                Title = notification.Title
                                //DefaultInput = "",
                                //PlaceholderContent = ""
                            });
                            break;
                    }
                }
                content.Actions = nativeActions;
            }
        }


        protected virtual async Task<ToastContent> CreateContent(Notification notification)
        {
            var content = new ToastContent
            {
                Duration = ToastDuration.Short,
                ActivationType = ToastActivationType.Foreground,
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

            await this.TrySetChannel(notification, content);
            return content;
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

//using System;
//using System.Threading;
//using Microsoft.Toolkit.Uwp.Notifications;
//using Windows.UI.Notifications;


//namespace Shiny.Notifications
//{
//    public class UwpPersistentNotification : IPersistentNotification
//    {
//        readonly ToastNotifier toastNotifier;
//        static int current;
//        int notificationId;
//        int sequence;
//        string channel;


//        public UwpPersistentNotification(Notification initialNotification)
//        {
//            this.toastNotifier = ToastNotificationManager.CreateToastNotifier();

//            this.title = initialNotification.Title;
//            this.message = initialNotification.Message;
//            this.channel = initialNotification.Channel ?? Channel.Default.Identifier;
//        }


//        public bool IsShowing { get; private set; }


//        string title;
//        public string Title
//        {
//            get => this.title;
//            set
//            {
//                if (this.title == value)
//                    return;

//                this.title = value;
//                this.Update();
//            }
//        }


//        string message;
//        public string Message
//        {
//            get => this.message;
//            set
//            {
//                if (this.message == value)
//                    return;

//                this.message = value;
//                this.Update();
//            }
//        }


//        bool ind;
//        public bool IsIndeterministic
//        {
//            get => this.ind;
//            set
//            {
//                if (this.ind == value)
//                    return;

//                this.ind = value;
//                if (this.IsShowing)
//                {
//                    this.Dismiss();
//                    this.Show();
//                }
//            }
//        }


//        double total;
//        public double Total
//        {
//            get => this.total;
//            set
//            {
//                if (this.total == value)
//                    return;

//                this.total = value;
//                this.Update();
//            }
//        }


//        double progress;
//        public double Progress
//        {
//            get => this.progress;
//            set
//            {
//                if (this.progress == value)
//                    return;

//                this.progress = value;
//                this.Update();
//            }
//        }


//        public void Show()
//        {
//            if (this.IsShowing)
//                throw new ArgumentException("Toast is already showing");

//            this.notificationId = ++current;
//            this.CreateToast();
//            this.IsShowing = true;
//        }
//        public void Dismiss() => ToastNotificationManager.History.Remove(this.notificationId.ToString());
//        public void Dispose() => this.Dismiss();


//        void Update()
//        {
//            Interlocked.Increment(ref this.sequence);
//            var data = new NotificationData
//            {
//                SequenceNumber = (uint)this.sequence
//            };
//            data.Values["Title"] = this.Title;
//            data.Values["Message"] = this.Message;

//            if (!this.IsIndeterministic && this.Total > 0)
//            {
//                data.Values["progressValue"] = (this.progress / this.total).ToString();
//                data.Values["progressValueString"] = $"{this.progress}/{this.total}";
//            }
//            this.toastNotifier.Update(
//                data,
//                this.notificationId.ToString(),
//                this.channel
//            );
//        }


//        void CreateToast()
//        {
//            var toastContent = new ToastContent
//            {
//                Duration = ToastDuration.Short,
//                ActivationType = ToastActivationType.Foreground,
//                Visual = new ToastVisual
//                {
//                    BindingGeneric = new ToastBindingGeneric
//                    {
//                        Children =
//                        {
//                            new AdaptiveText
//                            {
//                                Text = new BindableString("Title")
//                            },
//                            new AdaptiveText
//                            {
//                                Text = new BindableString("Message")
//                            }
//                        }
//                    }
//                }
//            };

//            if (this.IsIndeterministic || this.Total > 0)
//            {
//                //Title = new BindableString("title"),
//                var progressBar = new AdaptiveProgressBar();

//                if (this.IsIndeterministic)
//                {
//                    progressBar.Value = AdaptiveProgressBarValue.Indeterminate;
//                }
//                else
//                {
//                    progressBar.Value = new BindableProgressBarValue("progressValue");
//                    progressBar.ValueStringOverride = new BindableString("progressValueString");
//                    progressBar.Status = new BindableString("progressStatus");
//                }
//                toastContent.Visual.BindingGeneric.Children.Add(progressBar);
//            }

//            this.toastNotifier.Show(new ToastNotification(toastContent.GetXml())
//            {
//                Tag = this.notificationId.ToString(),
//                Group = this.channel
//            });
//            this.Update();
//        }
//    }
//}