using Shiny.Logging.Sqlite;

namespace Sample;


public class ErrorLoggingViewModel : ViewModel
{
    public ErrorLoggingViewModel(BaseServices services) : base(services)
    {
        this.Load = ReactiveCommand.CreateFromTask(async () =>
        {
            this.IsBusy = true;
            var results = await LoggingSqliteConnection.Instance!
                .Table<LogStore>()
                .Where(x => x.TimestampUtc >= DateTime.Now.Date)
                .OrderByDescending(x => x.TimestampUtc)
                .ToListAsync();

            this.Logs = results
                .Select(x =>
                {
                    // hack
                    x.TimestampUtc = x.TimestampUtc.ToLocalTime();
                    return x;
                })
                .ToList();
            
            this.IsBusy = false;
        });

        this.Clear = ReactiveCommand.CreateFromTask(async () =>
        {
            var result = await this.Dialogs.DisplayAlertAsync("Confirm", "Clear Logs?", "Yes", "No");
            if (result)
            {
                await LoggingSqliteConnection.Instance!.Logs.DeleteAsync();
                this.Load.Execute(null);
            }
        });
    }


    public ICommand Load { get; }
    public ICommand Clear { get; }
    [Reactive] public IList<LogStore> Logs { get; private set; }


    public override void OnAppearing()
    {
        base.OnAppearing();
        this.Load.Execute(null);
    }
}