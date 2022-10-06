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

            this.EventCount = this.Events.Count;
        });
    }


    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.Load.Execute(null);

        this.activityManager?
            .WhenActivityChanged()
            .SubOnMainThread(
                x => this.CurrentActivity = $"({x.Confidence}) {x.Types}",
                ex => { } // TODO
            )
            .DisposedBy(this.DestroyWith);

        this.WhenAnyProperty(x => x.Date)
            .DistinctUntilChanged()
            .Subscribe(_ => this.Load.Execute(null))
            .DisposedBy(this.DestroyWith);

        return base.InitializeAsync(parameters);
    }


    public ICommand Load { get; }
    [Reactive] public DateTime Date { get; set; } = DateTime.Now;
    [Reactive] public int EventCount { get; private set; }
    [Reactive] public string CurrentActivity { get; private set; }
    [Reactive] public IList<CommandItem> Events { get; private set; }
}
