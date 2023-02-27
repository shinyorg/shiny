using System.Reactive.Disposables;
using Shiny;
using Shiny.Locations;

namespace Sample.MotionActivity;


public class QueryViewModel : ViewModel
{
    readonly IMotionActivityManager activityManager;


    public QueryViewModel(BaseServices services, IMotionActivityManager activityManager) : base(services)
    {
        this.activityManager = activityManager;

        this.Load = ReactiveCommand.CreateFromTask(async () =>
        {
            var result = await this.activityManager.RequestAccess();

            if (result != AccessState.Available)
            {
                await this.Alert("Motion Activity is not available - " + result);
                return;
            }
            var activities = await this.activityManager.QueryByDate(this.Date);
            this.Events = activities
                .OrderByDescending(x => x.Timestamp)
                .Select(x => new CommandItem
                {
                    Text = $"({x.Confidence}) {x.Types}",
                    Detail = $"{x.Timestamp.LocalDateTime}"
                })
                .ToList();

            this.EventCount = activities.Count;
        });
    }


    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.Load.Execute(null);

        this.activityManager?
            .WhenActivityChanged()
            .SubOnMainThread(
                x =>
                {
                    this.CurrentActivity = $"({x.Confidence}) {x.Types}";
                },
                async ex => await this.Dialogs.DisplayAlertAsync("ERROR", ex.ToString(), "OK")
            )
            .DisposedBy(this.DestroyWith);

        this.WhenAnyProperty(x => x.Date)
            .Skip(1)
            .DistinctUntilChanged()
            .Subscribe(_ => this.Load.Execute(null))
            .DisposedBy(this.DestroyWith);

        return Task.CompletedTask;
    }


    public ICommand Load { get; }
    [Reactive] public DateTime Date { get; set; } = DateTime.Now;
    [Reactive] public int EventCount { get; private set; }
    [Reactive] public string CurrentActivity { get; private set; } = "None";
    [Reactive] public IList<CommandItem> Events { get; private set; }
}
