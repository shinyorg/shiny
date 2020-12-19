using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Samples.Infrastructure;
using Samples.Models;


namespace Samples.HttpTransfers
{
    public class LogViewModel : AbstractLogViewModel<HttpEvent>
    {
        readonly SampleSqliteConnection conn;


        public LogViewModel(IDialogs dialogs, SampleSqliteConnection conn) : base(dialogs)
            => this.conn = conn;


        protected override Task ClearLogs() => this.conn.DeleteAllAsync<HttpEvent>();

        protected override async Task<IEnumerable<HttpEvent>> LoadLogs() => await this.conn
            .HttpEvents
            .OrderByDescending(x => x.DateCreated)
            .ToListAsync();
    }
}
