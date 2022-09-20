namespace Sample.Notifications.Create;


public class IntervalViewModel : ViewModel
{
    public IntervalViewModel(BaseServices services) : base(services)
    {
        this.Cancel = ReactiveCommand.CreateFromTask(async () => await this.Navigation.GoBack());

        this.Use = ReactiveCommand.CreateFromTask(async () =>
        {
            var trigger = new Shiny.Notifications.IntervalTrigger
            {
                TimeOfDay = new TimeSpan(0, this.TimeOfDayHour, this.TimeOfDayMinutes, 0)
            };
            if (this.SelectedDay != null)
                trigger.DayOfWeek = (DayOfWeek)this.SelectedDay.Value.Value;
            
            State.CurrentNotification!.RepeatInterval = trigger;
            State.CurrentNotification!.Geofence = null;
            State.CurrentNotification!.ScheduleDate = null;
            
            await this.Navigation.GoBack();
        });
        
        this.Days = Enum
            .GetNames(typeof(DayOfWeek))
            .Select((name, index) => new Item(name, index))
            .ToArray();
    }


    public ICommand Use { get; }
    public ICommand Cancel { get; }

    public Item? SelectedDay { get; set; }
    public Item[] Days { get; }

    public int TimeOfDayHour { get; set; } = 8;
    public int TimeOfDayMinutes { get; set; } = 0;
}


public struct Item
{
    public Item(string text, int value)
    {
        this.Text = text;
        this.Value = value;
    }
    public string Text { get; }
    public int Value { get; }
}
