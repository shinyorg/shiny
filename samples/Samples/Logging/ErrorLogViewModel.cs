using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Prism.Navigation;
using ReactiveUI;
using Samples.Infrastructure;
using Shiny;
using Shiny.Infrastructure;
using Shiny.Integrations.Sqlite;
using Shiny.Logging;


namespace Samples.Logging
{
    public class ErrorLogViewModel : AbstractLogViewModel<CommandItem>
    {
        readonly ShinySqliteConnection conn;
        readonly ISerializer serializer;
        readonly INavigationService navigator;


        public ErrorLogViewModel(ShinySqliteConnection conn,
                                 ISerializer serializer,
                                 IDialogs dialogs,
                                 INavigationService navigator) : base(dialogs)
        {
            this.conn = conn;
            this.serializer = serializer;
            this.navigator = navigator;
        }


        public override void OnAppearing()
        {
            base.OnAppearing();
            Log
                .WhenExceptionLogged()
                .Select(x => this.ToItem(
                    DateTime.UtcNow,
                    x.Exception.ToString(),
                    () => x.Parameters
                ))
                .SubOnMainThread(this.InsertItem)
                .DisposeWith(this.DeactivateWith);
        }

        protected override Task ClearLogs() => this.conn.Logs.DeleteAsync(x => x.IsError);
        protected override async Task<IEnumerable<CommandItem>> LoadLogs()
        {
            var results = await this.conn
                .Logs
                .Where(x => x.IsError)
                .OrderByDescending(x => x.TimestampUtc)
                .ToListAsync();

            return results.Select(x => this.ToItem(
                x.TimestampUtc,
                x.Description,
                () =>
                {
                    if (x.Parameters.IsEmpty())
                        return null;

                    return this.serializer.Deserialize<Dictionary<string, string>>(x.Parameters);
                }
            ));
        }


        CommandItem ToItem(DateTime date, string exception, Func<IDictionary<string, string>?> getParameters)
        {
            var title = date.ToLocalTime().ToString("MMM dd, hh:dd:ss tt");
            return new CommandItem
            {
                Text = title,
                Detail = exception,
                PrimaryCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    var s = $"{title}{Environment.NewLine}{exception}{Environment.NewLine}";
                    var parameters = getParameters();

                    if (!parameters.Any())
                        foreach (var p in parameters)
                            s += $"{Environment.NewLine}{p.Key}: {p.Value}";

                    await navigator.ShowBigText(s, title);
                })
            };
        }
    }
}
