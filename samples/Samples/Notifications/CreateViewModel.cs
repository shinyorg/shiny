using System;
using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Xamarin.Forms;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny.Notifications;
using Shiny;
using Samples.Infrastructure;
using Prism.Navigation;


namespace Samples.Notifications
{
    public class CreateViewModel : ViewModel
    {
        readonly INotificationManager notificationManager;


        public CreateViewModel(INavigationService navigator,
                               INotificationManager notificationManager,
                               IDialogs dialogs)
        {
            this.notificationManager = notificationManager;
            this.NavToChannels = navigator.NavigateCommand("NotificationChannels");

            this.WhenAnyValue
            (
                x => x.SelectedDate,
                x => x.SelectedTime
            )
            .Select(x => new DateTime(
                x.Item1.Year,
                x.Item1.Month,
                x.Item1.Day,
                x.Item2.Hours,
                x.Item2.Minutes,
                x.Item2.Seconds)
            )
            .ToPropertyEx(this, x => x.ScheduledTime)
            .DisposeWith(this.DestroyWith);

            this.SelectedDate = DateTime.Now;
            this.SelectedTime = DateTime.Now.TimeOfDay.Add(TimeSpan.FromMinutes(10));

            this.SendNow = ReactiveCommand.CreateFromTask(() => this.BuildAndSend(
                "Test Now",
                "This is a test of the sendnow stuff",
                null
            ));
            this.Send = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    await this.BuildAndSend(
                        this.NotificationTitle,
                        this.NotificationMessage,
                        this.ScheduledTime
                    );
                    await dialogs.Alert("Notification Sent Successfully");
                },
                this.WhenAny(
                    x => x.NotificationTitle,
                    x => x.NotificationMessage,
                    x => x.ScheduledTime,
                    (title, msg, sch) =>
                        !title.GetValue().IsEmpty() &&
                        !msg.GetValue().IsEmpty() &&
                        sch.GetValue() > DateTime.Now
                )
            );
            this.PermissionCheck = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await notificationManager.RequestAccess();
                await dialogs.Snackbar("Permission Check Result: " + result);
            });

            this.StartChat = ReactiveCommand.CreateFromTask(() =>
                notificationManager.Send(
                    "Shiny Chat",
                    "Hi, What's your name?",
                    "ChatName",
                    DateTime.Now.AddSeconds(10)
                )
            );
        }


        async Task BuildAndSend(string title, string message, DateTime? scheduleDate)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                BadgeCount = this.BadgeCount,
                ScheduleDate = scheduleDate,
                Channel = this.Channel
            };
            if (Int32.TryParse(this.Identifier, out var id))
            {
                notification.Id = id;
            }
            if (!this.Payload.IsEmpty())
            {
                notification.Payload = new Dictionary<string, string> {
                    { nameof(this.Payload), this.Payload }
                };
            }
            notification.Android.UseBigTextStyle = this.UseAndroidBigTextStyle;

            await this.notificationManager.Send(notification);
            this.Reset();
        }

        public ICommand NavToChannels { get; }
        public ICommand PermissionCheck { get; }
        public ICommand Send { get; }
        public ICommand SendNow { get; }
        public ICommand StartChat { get; }

        public DateTime ScheduledTime { [ObservableAsProperty] get; }
        [Reactive] public string Identifier { get; set; }
        [Reactive] public string NotificationTitle { get; set;} = "Test Title";
        [Reactive] public string NotificationMessage { get; set; } = "Test Message";
        [Reactive] public bool UseActions { get; set; } = true;
        [Reactive] public DateTime SelectedDate { get; set; }
        [Reactive] public TimeSpan SelectedTime { get; set; }
        [Reactive] public int BadgeCount { get; set; }
        [Reactive] public string Payload { get; set; }
        [Reactive] public bool UseAndroidBigTextStyle { get; set; }
        [Reactive] public string Channel { get; set; }
        [Reactive] public string[] Channels { get; private set; }

        public bool IsAndroid => Device.RuntimePlatform == Device.Android;


        public async override void OnAppearing()
        {
            base.OnAppearing();
            this.Channels = (await this.notificationManager.GetChannels())
                .Select(x => x.Identifier)
                .ToArray();
        }


        void Reset()
        {
            this.Identifier = String.Empty;
            this.Payload = String.Empty;
            this.Channel = null;
        }
    }
}