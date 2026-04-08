using Shiny.Locations;
using Shiny.Notifications;

namespace Sample.Shared.Maui.Pages.Notifications;

[ShellMap<NotificationsPage>("notifications")]
public partial class NotificationsViewModel(
    INotificationManager notifications,
    INavigator navigator,
    IGpsManager? gpsManager = null
) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] string status = string.Empty;
    [ObservableProperty] int pendingCount;

    [ObservableProperty] string notifTitle = "Test Notification";
    [ObservableProperty] string notifMessage = "Hello from Shiny!";
    [ObservableProperty] string thread = string.Empty;

    [ObservableProperty] int selectedChannelIndex;

    public List<string> ChannelNames
    {
        get;
        private set
        {
            field = value;
            this.OnPropertyChanged();
        }
    } = [];

    public enum TriggerKind { Immediate, Scheduled, Geofence, Repeating }

    static readonly List<(string Label, TriggerKind Kind)> SupportedTriggers = BuildSupportedTriggers();

    static List<(string Label, TriggerKind Kind)> BuildSupportedTriggers()
    {
        var list = new List<(string, TriggerKind)>
        {
            ("Immediate", TriggerKind.Immediate),
            ("Scheduled", TriggerKind.Scheduled)
        };
#if IOS || ANDROID
        // Geofence triggers require Shiny.Locations and only Apple/Android notification managers handle them
        list.Add(("Geofence", TriggerKind.Geofence));
#endif
        list.Add(("Repeating", TriggerKind.Repeating));
        return list;
    }

    public List<string> NotificationTypes { get; } = SupportedTriggers.Select(x => x.Label).ToList();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsScheduled))]
    [NotifyPropertyChangedFor(nameof(IsGeofence))]
    [NotifyPropertyChangedFor(nameof(IsRepeating))]
    int selectedTypeIndex;

    TriggerKind CurrentTrigger =>
        this.SelectedTypeIndex >= 0 && this.SelectedTypeIndex < SupportedTriggers.Count
            ? SupportedTriggers[this.SelectedTypeIndex].Kind
            : TriggerKind.Immediate;

    public bool IsScheduled => this.CurrentTrigger == TriggerKind.Scheduled;
    public bool IsGeofence => this.CurrentTrigger == TriggerKind.Geofence;
    public bool IsRepeating => this.CurrentTrigger == TriggerKind.Repeating;

    [ObservableProperty] DateTime scheduleDate = DateTime.Today.AddDays(1);
    [ObservableProperty] TimeSpan scheduleTime = new(9, 0, 0);

    [ObservableProperty] string geoLatitude = string.Empty;
    [ObservableProperty] string geoLongitude = string.Empty;
    [ObservableProperty] string geoRadius = "200";
    [ObservableProperty] bool geoRepeat;

    public List<string> RepeatModes { get; } = ["Time of Day", "Interval"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRepeatTimeOfDay))]
    [NotifyPropertyChangedFor(nameof(IsRepeatInterval))]
    int selectedRepeatModeIndex;

    public bool IsRepeatTimeOfDay => this.SelectedRepeatModeIndex == 0;
    public bool IsRepeatInterval => this.SelectedRepeatModeIndex == 1;

    [ObservableProperty] TimeSpan repeatTimeOfDay = new(9, 0, 0);
    [ObservableProperty] string repeatIntervalMinutes = "60";

    public List<string> DayOfWeekOptions { get; } = ["(Any)", "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
    [ObservableProperty] int selectedDayOfWeekIndex;

    public bool IsAndroid => Microsoft.Maui.Devices.DeviceInfo.Platform == DevicePlatform.Android;
    public bool IsIos => Microsoft.Maui.Devices.DeviceInfo.Platform == DevicePlatform.iOS;

    [ObservableProperty] bool androidAutoCancel = true;
    [ObservableProperty] bool androidOnGoing;
    [ObservableProperty] bool androidUseBigTextStyle;
    [ObservableProperty] string androidSmallIcon = string.Empty;
    [ObservableProperty] string androidCategory = string.Empty;
    [ObservableProperty] string androidTicker = string.Empty;

    [ObservableProperty] string iosSubtitle = string.Empty;
    [ObservableProperty] string iosRelevanceScore = "0";

    public List<PendingNotificationViewModel> PendingNotifications
    {
        get;
        private set
        {
            field = value;
            this.OnPropertyChanged();
        }
    } = [];

    public void OnAppearing()
    {
        this.RefreshChannels();
        _ = this.RefreshPending();
    }

    public void OnDisappearing() { }

    void RefreshChannels()
    {
        this.ChannelNames = notifications.GetChannels().Select(ch => ch.Identifier).ToList();
        if (this.ChannelNames.Count > 0)
            this.SelectedChannelIndex = 0;
    }

    [RelayCommand]
    async Task RequestAccess()
    {
        var result = await notifications.RequestAccess(AccessRequestFlags.All);
        this.Status = $"Access: {result}";
    }

    [RelayCommand]
    Task GoToChannels() => navigator.NavigateTo("notificationchannels");

    public bool IsGpsAvailable => gpsManager != null;

    [RelayCommand]
    async Task UseCurrentLocation()
    {
        if (gpsManager == null)
        {
            this.Status = "GPS not available on this platform";
            return;
        }

        var access = await gpsManager.RequestAccess(new GpsRequest());
        if (access != AccessState.Available)
        {
            this.Status = $"GPS Access: {access}";
            return;
        }

        var reading = await gpsManager.GetLastReading();
        if (reading == null)
        {
            this.Status = "No GPS reading available";
            return;
        }

        this.GeoLatitude = reading.Position.Latitude.ToString("F6");
        this.GeoLongitude = reading.Position.Longitude.ToString("F6");
        this.Status = "Location set from GPS";
    }

    [RelayCommand]
    async Task Send()
    {
        if (string.IsNullOrWhiteSpace(this.NotifMessage))
        {
            this.Status = "Message is required";
            return;
        }

        var notification = this.CreateNotification();

        if (this.SelectedChannelIndex >= 0 && this.SelectedChannelIndex < this.ChannelNames.Count)
            notification.Channel = this.ChannelNames[this.SelectedChannelIndex];

        if (!string.IsNullOrWhiteSpace(this.Thread))
            notification.Thread = this.Thread;

        switch (this.CurrentTrigger)
        {
            case TriggerKind.Scheduled:
                notification.ScheduleDate = new DateTimeOffset(
                    this.ScheduleDate.Year, this.ScheduleDate.Month, this.ScheduleDate.Day,
                    this.ScheduleTime.Hours, this.ScheduleTime.Minutes, this.ScheduleTime.Seconds,
                    DateTimeOffset.Now.Offset
                );
                break;

            case TriggerKind.Geofence:
                if (!double.TryParse(this.GeoLatitude, out var lat) || !double.TryParse(this.GeoLongitude, out var lng))
                {
                    this.Status = "Enter valid latitude and longitude";
                    return;
                }
                if (!double.TryParse(this.GeoRadius, out var radius) || radius <= 0)
                {
                    this.Status = "Enter a valid radius";
                    return;
                }
                notification.Geofence = new GeofenceTrigger
                {
                    Center = new Position(lat, lng),
                    Radius = Distance.FromMeters(radius),
                    Repeat = this.GeoRepeat
                };
                break;

            case TriggerKind.Repeating:
                var trigger = new IntervalTrigger();
                if (this.SelectedRepeatModeIndex == 0)
                {
                    trigger.TimeOfDay = this.RepeatTimeOfDay;
                    if (this.SelectedDayOfWeekIndex > 0)
                        trigger.DayOfWeek = (DayOfWeek)(this.SelectedDayOfWeekIndex - 1);
                }
                else
                {
                    if (!double.TryParse(this.RepeatIntervalMinutes, out var mins) || mins <= 0)
                    {
                        this.Status = "Enter a valid interval in minutes";
                        return;
                    }
                    trigger.Interval = TimeSpan.FromMinutes(mins);
                }
                notification.RepeatInterval = trigger;
                break;
        }

        try
        {
            var access = await notifications.RequestRequiredAccess(notification);
            if (access != AccessState.Available)
            {
                this.Status = $"Access: {access}";
                return;
            }

            await notifications.Send(notification);
            this.Status = "Notification sent!";
            await this.RefreshPending();
        }
        catch (Exception ex)
        {
            this.Status = $"Error: {ex.Message}";
        }
    }

    Shiny.Notifications.Notification CreateNotification()
    {
        Shiny.Notifications.Notification notification;

#if ANDROID
        notification = new AndroidNotification
        {
            AutoCancel = this.AndroidAutoCancel,
            OnGoing = this.AndroidOnGoing,
            UseBigTextStyle = this.AndroidUseBigTextStyle,
            SmallIconResourceName = string.IsNullOrWhiteSpace(this.AndroidSmallIcon) ? null : this.AndroidSmallIcon,
            Category = string.IsNullOrWhiteSpace(this.AndroidCategory) ? null : this.AndroidCategory,
            Ticker = string.IsNullOrWhiteSpace(this.AndroidTicker) ? null : this.AndroidTicker
        };
#elif IOS
        notification = new AppleNotification
        {
            Subtitle = string.IsNullOrWhiteSpace(this.IosSubtitle) ? null : this.IosSubtitle,
            RelevanceScore = double.TryParse(this.IosRelevanceScore, out var score) ? score : 0
        };
#else
        notification = new Shiny.Notifications.Notification();
#endif

        notification.Title = this.NotifTitle;
        notification.Message = this.NotifMessage;
        return notification;
    }

    [RelayCommand]
    async Task CancelNotification(int id)
    {
        await notifications.Cancel(id);
        await this.RefreshPending();
    }

    [RelayCommand]
    async Task CancelAll()
    {
        await notifications.Cancel();
        await this.RefreshPending();
        this.Status = "All cancelled";
    }

    async Task RefreshPending()
    {
        var list = await notifications.GetPendingNotifications();
        this.PendingNotifications = list.Select(n => new PendingNotificationViewModel
        {
            Id = n.Id,
            Title = n.Title ?? "(no title)",
            Message = n.Message ?? "(no message)",
            TriggerInfo = GetTriggerInfo(n)
        }).ToList();
        this.PendingCount = list.Count;
    }

    static string GetTriggerInfo(Shiny.Notifications.Notification n)
    {
        if (n.ScheduleDate != null) return $"Scheduled: {n.ScheduleDate:g}";
        if (n.Geofence != null) return $"Geofence: {n.Geofence.Center?.Latitude:F4}, {n.Geofence.Center?.Longitude:F4}";
        if (n.RepeatInterval != null)
        {
            if (n.RepeatInterval.TimeOfDay != null)
                return $"Repeat: {n.RepeatInterval.TimeOfDay:hh\\:mm} {n.RepeatInterval.DayOfWeek?.ToString() ?? "daily"}";
            return $"Repeat: every {n.RepeatInterval.Interval?.TotalMinutes:F0} min";
        }
        return "Immediate";
    }
}

public partial class PendingNotificationViewModel : ObservableObject
{
    [ObservableProperty] int id;
    [ObservableProperty] string title = string.Empty;
    [ObservableProperty] string message = string.Empty;
    [ObservableProperty] string triggerInfo = string.Empty;
}
