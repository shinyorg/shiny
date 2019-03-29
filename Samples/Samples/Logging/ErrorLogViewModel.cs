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
using Shiny;
using Shiny.Infrastructure;
using Shiny.Logging;


namespace Samples.Logging
{
    public class ErrorLogViewModel : AbstractLogViewModel<CommandItem>
    {
        readonly SampleSqliteConnection conn;
        readonly ISerializer serializer;


        public ErrorLogViewModel(SampleSqliteConnection conn,
                                  ISerializer serializer,
                                  IUserDialogs dialogs) : base(dialogs)
        {
            this.conn = conn;
            this.serializer = serializer;

            Log
                .WhenExceptionLogged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => new CommandItem
                {
                    Text = DateTime.Now.ToString(),
                    Detail = x.Exception.ToString(),
                    PrimaryCommand = ReactiveCommand.Create(() =>
                    {
                        var s = $"{x.Exception}{Environment.NewLine}";
                        foreach (var p in x.Parameters)
                            s += $"{Environment.NewLine}{p.Key}: {p.Value}";

                        this.Dialogs.Alert(s);
                    })
                })
                .Subscribe(this.InsertItem)
                .DisposeWith(this.DestroyWith);
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
                {
                    var s = $"{x.Timestamp}{Environment.NewLine}{x.Description}{Environment.NewLine}";
                    if (!x.Parameters.IsEmpty())
                    {
                        var parameters = this.serializer.Deserialize<Tuple<string, string>[]>(x.Parameters);
                        foreach (var p in parameters)
                            s += $"{Environment.NewLine}{p.Item1}: {p.Item2}";
                    }
                    this.Dialogs.Alert(s);
                })
            });
        }
    }
}
