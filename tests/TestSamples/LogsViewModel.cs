namespace TestSamples;


public class LogsViewModel : ReactiveObject, IPageLifecycleAware
{
    public LogsViewModel(SampleSqlConnection conn)
    {
        this.Load = ReactiveCommand.CreateFromTask(async () =>
        {
            this.IsBusy = true;
            this.Logs = await conn.Logs.OrderBy(x => x.Timestamp).ToListAsync();
            this.IsBusy = false;
        });
    }


    public ICommand Load { get; }
    [Reactive] public bool IsBusy { get; private set; }
    [Reactive] public IList<Log> Logs { get; private set; }

    public void OnAppearing() => this.Load.Execute(null);
    public void OnDisappearing() { }
}
