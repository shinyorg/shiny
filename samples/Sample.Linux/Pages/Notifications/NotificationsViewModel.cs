namespace Sample.Linux.Pages.Notifications;

[ShellMap<NotificationsPage>("notifications")]
public partial class NotificationsViewModel(Shiny.Notifications.INotificationManager notifications) : ObservableObject
{
    [ObservableProperty] string access = "—";
    [ObservableProperty] string title = "Hello from Shiny";
    [ObservableProperty] string message = "This is a Linux desktop notification";
    [ObservableProperty] string status = "—";


    [RelayCommand]
    async Task RequestAccess()
    {
        var result = await notifications.RequestAccess();
        this.Access = result.ToString();
    }


    [RelayCommand]
    async Task SendNow()
    {
        try
        {
            await notifications.Send(new Shiny.Notifications.Notification
            {
                Title = this.Title,
                Message = this.Message
            });
            this.Status = $"Sent at {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            this.Status = $"Error: {ex.Message}";
        }
    }


    [RelayCommand]
    async Task SendScheduled()
    {
        try
        {
            await notifications.Send(new Shiny.Notifications.Notification
            {
                Title = this.Title,
                Message = this.Message + " (scheduled)",
                ScheduleDate = DateTime.Now.AddSeconds(15)
            });
            this.Status = $"Scheduled for {DateTime.Now.AddSeconds(15):HH:mm:ss}";
        }
        catch (Exception ex)
        {
            this.Status = $"Error: {ex.Message}";
        }
    }


    [RelayCommand]
    async Task CancelAll()
    {
        await notifications.Cancel(CancelScope.All);
        this.Status = "All cancelled";
    }
}
