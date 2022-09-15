namespace Sample;


public class LogsViewModel : ViewModel
{
    public LogsViewModel(BaseServices services, SampleSqliteConnection conn) : base(services)
    {
        this.Load = ReactiveCommand.CreateFromTask(async () =>
        {
            this.IsBusy = true;
            this.Logs = await conn
                .Logs
                .OrderByDescending(x => x.Timestamp)
                .ToListAsync();
            this.IsBusy = false;
        });
    }


    public ICommand Load { get; }
    [Reactive] public IList<Log> Logs { get; private set; }


    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.Load.Execute(null);
        return base.InitializeAsync(parameters);
    }
}
