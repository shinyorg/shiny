using Shiny.Notifications;

namespace Sample.Shared.Maui.Pages.Notifications;

[ShellMap<PendingNotificationsPage>("pendingnotifications")]
public partial class PendingNotificationsViewModel(INotificationManager notifications) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] string status = string.Empty;

    public List<PendingNotificationItemViewModel> Items
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    } = [];

    public void OnAppearing() => _ = this.Refresh();
    public void OnDisappearing() { }

    [RelayCommand]
    async Task Refresh()
    {
        var list = await notifications.GetPendingNotifications();
        var items = list
            .Select(n => new PendingNotificationItemViewModel
            {
                Id = n.Id,
                Title = string.IsNullOrWhiteSpace(n.Title) ? "(no title)" : n.Title!,
                Message = string.IsNullOrWhiteSpace(n.Message) ? "(no message)" : n.Message!,
                TriggerInfo = GetTriggerInfo(n)
            })
            .ToList();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            this.Items = items;
            this.Status = items.Count == 0 ? "No pending notifications" : $"{items.Count} pending";
        });
    }

    [RelayCommand]
    async Task CancelAll()
    {
        await notifications.Cancel();
        await this.Refresh();
        this.Status = "All cancelled";
    }

    [RelayCommand]
    async Task Cancel(int id)
    {
        await notifications.Cancel(id);
        await this.Refresh();
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

public partial class PendingNotificationItemViewModel : ObservableObject
{
    [ObservableProperty] int id;
    [ObservableProperty] string title = string.Empty;
    [ObservableProperty] string message = string.Empty;
    [ObservableProperty] string triggerInfo = string.Empty;
}
