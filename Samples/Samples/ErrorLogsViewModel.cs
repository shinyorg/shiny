using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Acr.UserDialogs;
using ReactiveUI;
using Samples.Infrastructure;
using Samples.Models;
using Shiny.Logging;


namespace Samples
{
    public class ErrorLogsViewModel : AbstractLogViewModel<CommandItem>
    {
        readonly SampleSqliteConnection conn;
        public ErrorLogsViewModel(SampleSqliteConnection conn, IUserDialogs dialogs) : base(dialogs)
        {
            this.conn = conn;
        }


        public override void OnAppearing()
        {
            base.OnAppearing();
            Log
                .WhenExceptionLogged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .InvokeCommand(this.Load)
                .DisposeWith(this.DeactivateWith);
        }

        protected override Task ClearLogs() => this.conn.DeleteAllAsync<ErrorLog>();


        protected override async Task<IEnumerable<CommandItem>> LoadLogs()
        {
            var results = await this.conn
                .Errors
                .OrderBy(x => x.Timestamp)
                .ToListAsync();

            return results.Select(x => new CommandItem
            {
                Text = x.Timestamp.ToString(),
                Detail = x.Description,
                PrimaryCommand = ReactiveCommand.Create(() =>
                    this.Dialogs.Alert(x.Description)
                )
            });
        }
    }
}
