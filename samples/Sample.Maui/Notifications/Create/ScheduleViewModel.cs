namespace Sample.Notifications.Create;


public class ScheduleViewModel : ViewModel
{

    public ScheduleViewModel(BaseServices services) : base(services)
    {
        this.SelectedDate = DateTime.Now;
        this.SelectedTime = DateTime.Now.TimeOfDay.Add(TimeSpan.FromMinutes(3));

        this.Use = new Command(async () =>
        {
            if (this.ScheduledDateTime < DateTime.Now)
            {
                await this.Dialogs.DisplayAlertAsync("ERROR", "Scheduled Date & Time must be in the future", "OK");
                return;
            }
            State.CurrentNotification!.ScheduleDate = this.ScheduledDateTime;
            State.CurrentNotification!.Geofence = null;
            State.CurrentNotification!.RepeatInterval = null;

            await this.Navigation.GoBack();
        });

    }


    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.WhenAnyProperty(x => x.SelectedDate)
            .Subscribe(_ => this.CalcDate())
            .DisposedBy(this.DestroyWith);

        this.WhenAnyProperty(x => x.SelectedTime)
            .Subscribe(_ => this.CalcDate())
            .DisposedBy(this.DestroyWith);

        return base.InitializeAsync(parameters);
    }


    public ICommand Use { get; }
    [Reactive] public DateTime SelectedDate { get; set; }
    [Reactive] public TimeSpan SelectedTime { get; set; }
    [Reactive] public DateTime ScheduledDateTime { get; private set; }


    void CalcDate()
    {
        this.ScheduledDateTime = new DateTime(
            this.SelectedDate.Year,
            this.SelectedDate.Month,
            this.SelectedDate.Day,
            this.SelectedTime.Hours,
            this.SelectedTime.Minutes,
            0
        );
    }
}
