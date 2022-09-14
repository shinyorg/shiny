using System;
using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;
using System.Reactive.Linq;
using Xamarin.Forms;
using Shiny.Notifications;
using Shiny;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;

namespace Sample.Create
{
    public class CreateViewModel : SampleViewModel
    {
        readonly INotificationManager notificationManager;


        public CreateViewModel()
        {
            State.CurrentNotification = new Notification();

            this.notificationManager = ShinyHost.Resolve<INotificationManager>();

            this.SetGeofence = this.NavigateCommand<LocationPage>(true);
            this.SetInterval = this.NavigateCommand<IntervalPage>(true);
            this.SetScheduleDate = this.NavigateCommand<SchedulePage>(true);
            this.SetNoTrigger = new Command(() =>
            {
                State.CurrentNotification.ScheduleDate = null;
                State.CurrentNotification.Geofence = null;
                State.CurrentNotification.RepeatInterval = null;
                this.TriggerDetails = null;
                this.IsTriggerVisible = false;
            });

            this.Send = this.LoadingCommand(async () =>
            {
                if (this.NotificationTitle.IsEmpty())
                {
                    await this.Alert("Title is required");
                    return;
                }
                if (this.NotificationMessage.IsEmpty())
                {
                    await this.Alert("Message is required");
                    return;
                }

                var n = State.CurrentNotification!;
                n.Title = this.NotificationTitle;
                n.Message = this.NotificationMessage;

                n.Thread = this.Thread;
                n.Channel = this.Channel;
                if (Int32.TryParse(this.Identifier, out var id))
                    n.Id = id;

                await this.TrySetDownload(n);
                if (!this.Payload.IsEmpty())
                {
                    n.Payload = new Dictionary<string, string> {
                        { nameof(this.Payload), this.Payload }
                    };
                }
                n.Android.UseBigTextStyle = this.UseAndroidBigTextStyle;

                var result = await notificationManager.RequestRequiredAccess(n);
                if (result != AccessState.Available)
                {
                    await this.Alert("Invalid Permission: " + result);
                }
                else
                {
                    await notificationManager.Send(n);
                    await this.Alert("Notification Sent");
                    await this.Navigation.PopAsync();
                }
            });
        }


        readonly HttpClient httpClient = new HttpClient();
        async Task TrySetDownload(Notification notification)
        {
            if (this.ImageUri.IsEmpty())
                return;

            var filePath = Path.GetTempFileName();
            using (var stream = await this.httpClient.GetStreamAsync(this.ImageUri))
            {
                using (var fs = File.Create(filePath))
                {
                    await stream.CopyToAsync(fs);
                }
                
            }
            notification.Attachment = new FileInfo(filePath);
        }


        public ICommand SetNoTrigger { get; }
        public ICommand SetScheduleDate { get; }
        public ICommand SetInterval { get; }
        public ICommand SetGeofence { get; }
        public ICommand Send { get; }


        string id;
        public string Identifier
        {
            get => this.id;
            set => this.Set(ref this.id, value);
        }


        string title = "Test Title";
        public string NotificationTitle
        {
            get => this.title;
            set => this.Set(ref this.title, value);
        }


        string msg = "Test Message";
        public string NotificationMessage
        {
            get => this.msg;
            set => this.Set(ref this.msg, value);
        }


        string imageUri = "https://github.com/shinyorg/shiny/blob/master/art/nuget.png";
        public string ImageUri
        {
            get => this.imageUri;
            set => this.Set(ref this.imageUri, value);
        }


        string thread;
        public string Thread
        {
            get => this.thread;
            set => this.Set(ref this.thread, value);
        }


        bool actions = true;
        public bool UseActions
        {
            get => this.actions;
            set => this.Set(ref this.actions, value);
        }


        string payload;
        public string Payload
        {
            get => this.payload;
            set => this.Set(ref this.payload, value);
        }


        bool big;
        public bool UseAndroidBigTextStyle
        {
            get => this.big;
            set => this.Set(ref this.big, value);
        }


        string? channel;
        public string? Channel
        {
            get => this.channel;
            set => this.Set(ref this.channel, value);
        }


        string? triggerDetails;
        public string? TriggerDetails
        {
            get => this.triggerDetails;
            private set => this.Set(ref this.triggerDetails, value);
        }


        bool isTriggerVisible;
        public bool IsTriggerVisible
        {
            get => this.isTriggerVisible;
            private set => this.Set(ref this.isTriggerVisible, value);
        }


        string[] channels;
        public string[] Channels
        {
            get => this.channels;
            private set
            {
                this.channels = value;
                this.RaisePropertyChanged();
            }
        }

        public bool IsAndroid => ShinyHost.Resolve<IPlatform>().IsAndroid();


        public async override void OnAppearing()
        {
            base.OnAppearing();

            this.Channels = (await this.notificationManager.GetChannels())
                .Select(x => x.Identifier)
                .ToArray();

            this.IsTriggerVisible = true;
            var n = State.CurrentNotification!;
            if (n.ScheduleDate != null)
            {
                this.TriggerDetails = $"Scheduled: {n.ScheduleDate:MMM dd, yyyy hh:mm tt}";
            }
            else if (n.RepeatInterval != null)
            {
                var ri = n.RepeatInterval;

                if (ri.Interval == null)
                {
                    this.TriggerDetails = $"Interval: {ri.DayOfWeek} Time: {ri.TimeOfDay}";
                }
                else
                {
                    this.TriggerDetails = $"Interval: {ri.Interval}";
                }
            }
            else if (n.Geofence != null)
            {
                this.TriggerDetails = $"Location Aware: {n.Geofence.Center!.Latitude} / {n.Geofence.Center!.Longitude}";
            }
            else
            {
                this.TriggerDetails = null;
                this.IsTriggerVisible = false;
            }
        }
    }
}