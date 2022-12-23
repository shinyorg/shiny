using Shiny.Notifications;
using Shiny;
using Notification = Shiny.Notifications.Notification;

namespace Sample.Notifications.Create;


public class CreateViewModel : ViewModel
{
    readonly INotificationManager notificationManager;


    public CreateViewModel(BaseServices services, INotificationManager notificationManager) : base(services)
    {
        State.CurrentNotification = new Notification();

        this.notificationManager = notificationManager;

        this.SetGeofence = this.Navigation.Command(nameof(LocationPage));
        this.SetInterval = this.Navigation.Command(nameof(IntervalPage));
        this.SetScheduleDate = this.Navigation.Command(nameof(SchedulePage));
        this.SetNoTrigger = new Command(() =>
        {
            State.CurrentNotification.ScheduleDate = null;
            State.CurrentNotification.Geofence = null;
            State.CurrentNotification.RepeatInterval = null;
            this.TriggerDetails = null;
            this.IsTriggerVisible = false;
        });

        this.Send = ReactiveCommand.CreateFromTask(async () =>
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
            //n.Android.UseBigTextStyle = this.UseAndroidBigTextStyle;

            var result = await notificationManager.RequestRequiredAccess(n);
            if (result != AccessState.Available)
            {
                await this.Alert("Invalid Permission: " + result);
            }
            else
            {
                await notificationManager.Send(n);
                await this.Dialogs.DisplayAlertAsync("", "Notification Sent", "OK");
                await this.Navigation.GoBack();
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
        notification.LocalAttachmentPath = filePath;
    }


    public ICommand SetNoTrigger { get; }
    public ICommand SetScheduleDate { get; }
    public ICommand SetInterval { get; }
    public ICommand SetGeofence { get; }
    public ICommand Send { get; }

    [Reactive] public string Identifier { get; set; }
    [Reactive] public string NotificationTitle { get; set; }
    [Reactive] public string NotificationMessage { get; set; } = "Test Message";
    [Reactive] public string ImageUri { get; set; } = "https://github.com/shinyorg/shiny/blob/master/art/nuget.png";
    [Reactive] public string Thread { get; set; }
    [Reactive] public bool UseActions { get; set; }
    [Reactive] public string Payload { get; set; }
    [Reactive] public string? TriggerDetails { get; set; }
    [Reactive] public bool IsTriggerVisible { get; set; }
    [Reactive] public string? Channel { get; set; }
    [Reactive] public string[] Channels { get; private set; }


    public override void OnNavigatedTo(INavigationParameters parameters)
    { 
        this.Channels = this.notificationManager
            .GetChannels()
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
        base.OnNavigatedTo(parameters);
    }
}