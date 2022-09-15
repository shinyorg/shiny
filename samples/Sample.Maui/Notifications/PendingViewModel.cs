using Shiny;
using Shiny.Notifications;

namespace Sample.Notifications;


public class PendingViewModel : ViewModel
{
    public PendingViewModel(BaseServices services, INotificationManager notifications) : base(services)
    {

        this.Create = this.NavigateCommand<Create.CreatePage>();

        this.Load = this.LoadingCommand(async () =>
        {
            var pending = await notifications.GetPendingNotifications();
            this.PendingList = pending
                .Select(x => new CommandItem
                {
                    Text = $"[{x.Id}] {x.Title}",
                    Detail = this.GetDetails(x),
                    PrimaryCommand = new Command(async () =>
                    {
                        await notifications.Cancel(x.Id);
                        this.Load!.Execute(null);
                    })
                })
                .ToList();
        });

        this.CancelAll = this.ConfirmCommand(
            "Cancel All Notifications?",
            async () =>
            {
                await notifications.Cancel();
                this.Load.Execute(null);
            }
        );
    }


    public ICommand Load { get; }
    public ICommand Create { get; }
    public ICommand CancelAll { get; }



    IList<CommandItem> pending;
    public IList<CommandItem> PendingList
    {
        get => this.pending;
        private set
        {
            this.pending = value;
            this.RaisePropertyChanged();
        }
    }


    public override void OnAppearing()
    {
        base.OnAppearing();
        this.Load.Execute(null);
    }


    string GetDetails(Notification notification)
    {
        if (notification.Geofence != null)
            return $"Geofence Trigger: {notification.Geofence.Center!.Latitude} / {notification.Geofence.Center!.Longitude}";

        if (notification.RepeatInterval != null)
            return $"Interval Trigger: {notification.RepeatInterval.DayOfWeek} - Hour: {notification.RepeatInterval.TimeOfDay}";

        if (notification.ScheduleDate != null)
            return $"Schedule Trigger: {notification.ScheduleDate}";

        return "No Trigger";
    }
}
