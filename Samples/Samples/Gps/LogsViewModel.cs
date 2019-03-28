using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive;
using System.Text;
using Acr.UserDialogs;
using ReactiveUI;
using Samples.Models;
using Humanizer;
using Samples.Infrastructure;


namespace Samples.Gps
{
    public class LogsViewModel : AbstractLogViewModel
    {
        readonly SampleSqliteConnection conn;
        public LogsViewModel(SampleSqliteConnection conn, IUserDialogs dialogs) : base(dialogs)
        {
            this.conn = conn;
        }


        protected override Task ClearLogs() => this.conn.DeleteAllAsync<GpsEvent>();


        protected override async Task<IEnumerable<CommandItem>> LoadLogs()
        {
            var list = await this.conn
                .GpsEvents
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            return list.Select(x => new CommandItem
            {
                Data = x,
                Text = $"{x.Date}",
                Detail = $"{x.Latitude} / {x.Longitude}",
                PrimaryCommand = ReactiveCommand.Create(() =>
                {
                    var msg = new StringBuilder()
                        .AppendLine("Lat: " + x.Latitude)
                        .AppendLine("Lng: " + x.Longitude)
                        .AppendLine("Alt: " + x.Altitude)
                        .AppendLine("Position Accuracy: " + x.PositionAccuracy)
                        .AppendLine("Heading: " + x.Heading.ToHeading())
                        .AppendLine("Heading Accuracy: " + x.HeadingAccuracy)
                        .AppendLine("Speed (m/s) " + x.Speed)
                        .ToString();

                    this.Dialogs.Alert(msg);
                })
            });
        }
    }
}
