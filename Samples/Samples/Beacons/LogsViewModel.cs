using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Models;


namespace Samples.Beacons
{
    public class LogsViewModel : ViewModel
    {
        readonly SampleSqliteConnection conn;


        public LogsViewModel(IUserDialogs dialogs, SampleSqliteConnection conn)
        {
            this.conn = conn;

            this.Purge = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await dialogs.ConfirmAsync("Are you sure you wish to purge all logs?");
                if (result)
                {
                    await this.conn.BeaconEvents.DeleteAsync();
                    await this.LoadData();
                }
            });
        }


        public ICommand Purge { get; }
        [Reactive] public IList<BeaconEvent> Pings { get; private set; }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.LoadData();
        }


        async Task LoadData()
        {
            this.Pings = await this.conn
                .BeaconEvents
                //.Select(x => new BeaconEvent
                //{
                //    Id = x.Id,
                //    Identifier = x.Identifier,
                //    Entered = x.Entered,
                //    Uuid = x.Uuid,
                //    Major = x.Major,
                //    Minor = x.Minor,
                //    Date = x.Date.ToLocalTime()
                //})
                .OrderByDescending(x => x.Date)
                .ToListAsync();
        }
    }
}
