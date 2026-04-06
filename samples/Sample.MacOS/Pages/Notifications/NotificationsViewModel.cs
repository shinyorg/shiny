using Shiny.Notifications;

namespace Sample.MacOS.Pages.Notifications;

[ShellMap<NotificationsPage>("notifications")]
public partial class NotificationsViewModel(INotificationManager notifications) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] string status = string.Empty;
    [ObservableProperty] int pendingCount;

    [ObservableProperty] string notifTitle = "Test Notification";
    [ObservableProperty] string notifMessage = "Hello from Shiny on macOS!";
    [ObservableProperty] string subtitle = string.Empty;
    [ObservableProperty] string delaySeconds = "0";

    public List<PendingNotificationViewModel> PendingNotifications
    {
        get;
        private set
        {
            field = value;
            this.OnPropertyChanged();
        }
    } = [];

    public void OnAppearing() => _ = this.RefreshPending();
    public void OnDisappearing() { }

    [RelayCommand]
    async Task RequestAccess()
    {
        var result = await notifications.RequestAccess(AccessRequestFlags.Notification);
        this.Status = $"Access: {result}";
    }

    [RelayCommand]
    async Task Send()
    {
        if (string.IsNullOrWhiteSpace(this.NotifMessage))
        {
            this.Status = "Message is required";
            return;
        }

        var notification = new AppleNotification
        {
            Title = this.NotifTitle,
            Message = this.NotifMessage,
            Subtitle = string.IsNullOrWhiteSpace(this.Subtitle) ? null : this.Subtitle
        };

        if (int.TryParse(this.DelaySeconds, out var delay) && delay > 0)
            notification.ScheduleDate = DateTimeOffset.Now.AddSeconds(delay);

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

    [RelayCommand]
    async Task CancelAll()
    {
        await notifications.Cancel();
        await this.RefreshPending();
        this.Status = "All cancelled";
    }

    [RelayCommand]
    async Task RefreshPending()
    {
        var list = await notifications.GetPendingNotifications();
        this.PendingNotifications = list.Select(n => new PendingNotificationViewModel
        {
            Id = n.Id,
            Title = n.Title ?? "(no title)",
            Message = n.Message ?? "(no message)",
            TriggerInfo = n.ScheduleDate != null ? $"Scheduled: {n.ScheduleDate:g}" : "Immediate"
        }).ToList();
        this.PendingCount = list.Count;
    }
}

public partial class PendingNotificationViewModel : ObservableObject
{
    [ObservableProperty] int id;
    [ObservableProperty] string title = string.Empty;
    [ObservableProperty] string message = string.Empty;
    [ObservableProperty] string triggerInfo = string.Empty;
}
