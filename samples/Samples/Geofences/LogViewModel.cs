using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Samples.Infrastructure;
using Samples.Models;


namespace Samples.Geofences
{
    public class LogViewModel : AbstractLogViewModel<CommandItem>
    {
        readonly SampleSqliteConnection conn;
        public LogViewModel(SampleSqliteConnection conn, IDialogs dialogs) : base(dialogs)
            => this.conn = conn;


        protected override Task ClearLogs() => this.conn.DeleteAllAsync<GeofenceEvent>();


        protected override async Task<IEnumerable<CommandItem>> LoadLogs()
        {
            var events = await conn
                .GeofenceEvents
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            return events.Select(x => new CommandItem
            {
                Text = $"[{x.Date.ToLocalTime()}] {x.Text}",
                Detail = x.Detail
            });
        }
    }
}
