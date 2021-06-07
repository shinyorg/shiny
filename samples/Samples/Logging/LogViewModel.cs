using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Prism.Navigation;
using ReactiveUI;
using Samples.Infrastructure;
using Shiny.Infrastructure;
using Shiny.Integrations.Sqlite;


namespace Samples.Logging
{
    public class LogViewModel : AbstractLogViewModel<CommandItem>
    {
        readonly ShinySqliteConnection conn;
        readonly ISerializer serializer;
        readonly INavigationService navigator;


        public LogViewModel(ShinySqliteConnection conn,
                            ISerializer serializer,
                            IDialogs dialogs,
                            INavigationService navigator) : base(dialogs)
        {
            this.conn = conn;
            this.serializer = serializer;
            this.navigator = navigator;
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
                _ = x.Parameters
            ));
        }


        CommandItem ToItem(DateTime date, string exception, string value)
        {
            var title = date.ToLocalTime().ToString("MMM dd, hh:dd:ss tt");
            return new CommandItem
            {
                Text = title,
                Detail = exception,
                PrimaryCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    var s = $"{title}{Environment.NewLine}{exception}{Environment.NewLine}{value}";
                    //var parameters = getParameters();

                    //if (parameters != null && parameters.Any())
                    //    foreach (var p in parameters)
                    //        s += $"{Environment.NewLine}{p.Key}: {p.Value}";

                    await this.navigator.ShowBigText(s, title);
                })
            };
        }
    }
}
