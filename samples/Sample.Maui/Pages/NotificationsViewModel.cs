using Shiny.Notifications;

namespace Sample.Maui.Pages;

[ShellMap<NotificationsPage>("notifications")]
public partial class NotificationsViewModel(INotificationManager notifications) : ObservableObject
{
    [ObservableProperty] string accessStatus = "Unknown";
    [ObservableProperty] int pendingCount;

    [RelayCommand]
    async Task RequestAccess()
    {
        var result = await notifications.RequestAccess();
        AccessStatus = result.ToString();
    }

    [RelayCommand]
    async Task SendNow()
    {
        await notifications.Send(new Shiny.Notifications.Notification
        {
            Title = "Shiny Sample",
            Message = "This is an immediate notification",
            Channel = "General"
        });
        await RefreshPending();
    }

    [RelayCommand]
    async Task SendScheduled()
    {
        await notifications.Send(new Shiny.Notifications.Notification
        {
            Title = "Shiny Sample",
            Message = "This is a scheduled notification",
            Channel = "General",
            ScheduleDate = DateTimeOffset.Now.AddSeconds(10)
        });
        await RefreshPending();
    }

    [RelayCommand]
    async Task CancelAll()
    {
        await notifications.Cancel();
        await RefreshPending();
    }

    async Task RefreshPending()
    {
        var list = await notifications.GetPendingNotifications();
        PendingCount = list.Count;
    }
}
