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
    public class EventsViewModel : AbstractLogViewModel<CommandItem>
    {
        readonly ShinySqliteConnection conn;
        readonly ISerializer serializer;
        readonly INavigationService navigator;


        public EventsViewModel(ShinySqliteConnection conn,
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
                .WhenEventLogged()
                .Select(x => this.ToItem(
                    x.EventName,
                    x.Description,
                    DateTime.UtcNow,
                    () => x.Parameters
                ))
                .SubOnMainThread(this.InsertItem)
                .DisposeWith(this.DeactivateWith);
        }


        protected override Task ClearLogs() => this.conn.Logs.DeleteAsync(x => !x.IsError);
        protected override async Task<IEnumerable<CommandItem>> LoadLogs()
        {
            var logs = await this.conn
                .Logs
                .OrderByDescending(x => x.TimestampUtc)
                .Where(x => !x.IsError)
                .ToListAsync();

            return logs.Select(x => this.ToItem(
                x.Description,
                x.Detail,
                x.TimestampUtc,
                () =>
                {
                    if (x.Parameters.IsEmpty())
                        return null;

                    return this.serializer.Deserialize<Dictionary<string, string>>(x.Parameters);
                })
            );
        }


        CommandItem ToItem(string description, string detail, DateTime dt, Func<IDictionary<string, string>?> getParameters)
        {
            var title = $"{description} ({dt.ToLocalTime():MMM dd, hh:dd:ss tt})";
            return new CommandItem
            {
                Text = title,
                Detail = detail,
                PrimaryCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    var s = $"{title}{Environment.NewLine}{detail}";
                    var parameters = getParameters();
                    if (!parameters.IsEmpty())
                    {
                        foreach (var p in parameters)
                            s += $"{Environment.NewLine}{p.Key}: {p.Value}";
                    }
                    await this.navigator.ShowBigText(s, title);
                })
            };
        }
    }
}
