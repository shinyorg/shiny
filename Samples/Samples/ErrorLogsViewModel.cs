using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Acr.UserDialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Models;
using Shiny.Logging;


namespace Samples
{
    public class ErrorLogsViewModel : ViewModel
    {
        public ErrorLogsViewModel(SampleSqliteConnection conn, IUserDialogs dialogs)
        {
            this.Load = ReactiveCommand.CreateFromTask(async () =>
            {
                var results = await conn
                    .Errors
                    .OrderByDescending(x => x.Timestamp)
                    .ToListAsync();

                this.Logs = results
                    .Select(x => new CommandItem
                    {
                        Text = x.Timestamp.ToString(),
                        Detail = x.Description,
                        PrimaryCommand = ReactiveCommand.Create(() =>
                            dialogs.Alert(x.Description)
                        )
                    })
                    .ToList();

                this.HasErrors = results.Any();
            });
            this.Clear = ReactiveCommand.CreateFromTask(async () =>
            {
                await conn.DeleteAllAsync<ErrorLog>();
                await this.Load.Execute();
            });
            this.BindBusyCommand(this.Load);
        }


        [Reactive] public bool HasErrors { get; private set; }
        [Reactive] public IList<CommandItem> Logs { get; private set; }
        public ReactiveCommand<Unit, Unit> Load { get; }
        public ReactiveCommand<Unit, Unit> Clear { get; }


        public override async void OnAppearing()
        {
            base.OnAppearing();
            await this.Load.Execute();
            Log
                .WhenExceptionLogged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .InvokeCommand(this.Load)
                .DisposeWith(this.DeactivateWith);
        }
    }
}
