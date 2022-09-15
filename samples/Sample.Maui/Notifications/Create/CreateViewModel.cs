using Shiny.Notifications;
using Shiny;

namespace Sample.Notifications.Create;

public class CreateViewModel : ViewModel
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