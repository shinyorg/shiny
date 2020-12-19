using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows.Input;
using Samples.Infrastructure;
using Samples.Models;
using Shiny.Push;


namespace Samples.Push
{
    public class EventListViewModel : AbstractLogViewModel<CommandItem>
    {
        readonly SampleSqliteConnection conn;
        readonly IPushManager? pushManager;


        public EventListViewModel(SampleSqliteConnection conn,
                                  IDialogs dialogs,
                                  IPushManager? pushManager = null) : base(dialogs)
        {
            this.conn = conn;
            this.pushManager = pushManager;
        }

        protected override Task ClearLogs() => this.conn.DeleteAllAsync<PushEvent>();
        protected override async Task<IEnumerable<CommandItem>> LoadLogs()
        {
            var data = await this.conn
                .PushEvents
                .OrderByDescending(x => x.Timestamp)
                .ToListAsync();

            return data.Select(x => new CommandItem
            {
                Text = x.Payload,
                Detail = x.Timestamp.ToLocalTime().ToString()
            });
        }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.pushManager?
                .WhenReceived()
                .SubOnMainThread(async x =>
                {
                    ((ICommand)this.Load).Execute(null);
                    await this.Dialogs.Snackbar("New Push Event");
                })
                .DisposeWith(this.DeactivateWith);
        }
    }
}
