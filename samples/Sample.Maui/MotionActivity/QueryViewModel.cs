using Shiny.Locations;

namespace Sample.MotionActivity;


public class QueryViewModel : ViewModel
{
    readonly IMotionActivityManager activityManager;
    IDisposable? sub;

    public QueryViewModel(BaseServices services, IMotionActivityManager activityManager) : base(services)
    {
        this.activityManager = activityManager;

        this.Load = ReactiveCommand.CreateFromTask(async () =>
        {
            Console.WriteLine("Loading");
            var result = await this.activityManager.RequestAccess();

            if (result != AccessState.Available)
            {
                await this.Alert("Motion Activity is not available - " + result);
                return;
            }

            var date = this.IsRealTime ? DateTimeOffset.UtcNow : this.Date;
            var activities = await this.activityManager.QueryByDate(date);

            this.Events = activities
                .OrderByDescending(x => x.Timestamp)
                .Select(x => new CommandItem
                {
                    Text = Stringify(x),
                    Detail = x.Timestamp.LocalDateTime.ToString("dddd, MMM dd - hh:mm:ss tt")
                })
                .ToList();

            this.EventCount = activities.Count;
        });
    }


    public override async Task InitializeAsync(INavigationParameters parameters)
    {
        this.activityManager?
            .WhenActivityChanged()
            .Where(_ => this.IsRealTime)
            .SubOnMainThread(
                _ => this.Load.Execute(null),
                async ex => await this.Dialogs.DisplayAlertAsync("ERROR", ex.ToString(), "OK")
            )
            .DisposedBy(this.DestroyWith);

        this.WhenAnyValue(x => x.IsRealTime)
            .Skip(1)
            .Subscribe(value =>
            {
                if (value)
                    this.Date = DateTime.Now;

                this.Load.Execute(null);
            })
            .DisposedBy(this.DestroyWith);


        this.WhenAnyProperty(x => x.Date)
            .Skip(1)
            .DistinctUntilChanged()
            .Subscribe(_ => this.Load.Execute(null))
            .DisposedBy(this.DestroyWith);
    }


    public override void OnAppearing()
    {
        base.OnAppearing();
        this.Load.Execute(null);
    }

    public ICommand Load { get; }
    [Reactive] public DateTime Date { get; set; } = DateTime.Now;
    [Reactive] public int EventCount { get; private set; }
    [Reactive] public IList<CommandItem> Events { get; private set; }
    [Reactive] public bool IsRealTime { get; set; } = true;


    static string Stringify(MotionActivityEvent e)
    {
        if (e.IsUnknown)
            return "Unknown";

        var s = "";
        for (var i = 0; i < e.DetectedActivities.Count; i++)
        {
            if (i > 0) s += ", ";

            var act = e.DetectedActivities[i];
            s += $"{act.ActivityType} ({act.Confidence})";
        }
        return s;
    }
}