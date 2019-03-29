using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Acr.UserDialogs;
using ReactiveUI;
using Humanizer;
using Shiny.Locations;
using Samples.Models;
using Samples.Infrastructure;


namespace Samples.Gps
{
    public class LogsViewModel : AbstractLogViewModel<CommandItem>
    {
        readonly SampleSqliteConnection conn;
        readonly IGpsManager manager;


        public LogsViewModel(SampleSqliteConnection conn,
                             IGpsManager manager,
                             IUserDialogs dialogs) : base(dialogs)
        {
            this.conn = conn;
            this.manager = manager;
        }


        protected override void OnStart()
        {
            base.OnStart();
            this.manager
                .WhenReading()
                .Select(x => new CommandItem
                {
                    Text = $"{x.Timestamp}",
                    Detail = $"{x.Position.Latitude} / {x.Position.Longitude}",
                    PrimaryCommand = ReactiveCommand.Create(() =>
                    {
                        var msg = new StringBuilder()
                            .AppendLine("Lat: " + x.Position.Latitude)
                            .AppendLine("Lng: " + x.Position.Longitude)
                            .AppendLine("Alt: " + x.Altitude)
                            .AppendLine("Position Accuracy: " + x.PositionAccuracy)
                            .AppendLine("Heading: " + x.Heading.ToHeading())
                            .AppendLine("Heading Accuracy: " + x.HeadingAccuracy)
                            .AppendLine("Speed (m/s) " + x.Speed)
                            .ToString();

                        this.Dialogs.Alert(msg);
                    })
                })
                .SubOnMainThread(this.InsertItem)
                .DisposeWith(this.DestroyWith);
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
