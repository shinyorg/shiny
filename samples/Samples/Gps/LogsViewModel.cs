using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using Humanizer;
using Shiny.Locations;
using Samples.Models;
using Samples.Infrastructure;
using Prism.Navigation;
using ReactiveUI.Fody.Helpers;


namespace Samples.Gps
{
    public class LogsViewModel : AbstractLogViewModel<CommandItem>
    {
        readonly SampleSqliteConnection conn;
        readonly IGpsManager manager;


        public LogsViewModel(SampleSqliteConnection conn,
                             IGpsManager manager,
                             IDialogs dialogs) : base(dialogs)
        {
            this.conn = conn;
            this.manager = manager;

            this.WhenAnyValue(x => x.SelectedDate)
                .Skip(1)
                .Subscribe(_ => this.Load.Execute(null));
        }


        public override void Initialize(INavigationParameters parameters)
        {
            base.Initialize(parameters);
            this.manager
                .WhenReading()
                .Where(x => x.Timestamp.Date == DateTime.Now.Date)
                .Select(x => new CommandItem
                {
                    Text = $"{x.Timestamp.ToLocalTime()}",
                    Detail = $"{x.Position.Latitude} / {x.Position.Longitude}",
                    PrimaryCommand = ReactiveCommand.CreateFromTask(async () =>
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

                        await this.Dialogs.Alert(msg);
                    })
                })
                .SubOnMainThread(this.InsertItem)
                .DisposeWith(this.DestroyWith);
        }


        [Reactive] public DateTime SelectedDate { get; set; } = DateTime.Now;
        protected override Task ClearLogs() => this.conn.DeleteAllAsync<GpsEvent>();


        protected override async Task<IEnumerable<CommandItem>> LoadLogs()
        {
            var start = this.SelectedDate.Date;
            var end = new DateTime(start.Year, start.Month, start.Day, 23, 59, 59);

            var list = await this.conn.QueryAsync<GpsEvent>(
                "SELECT * FROM GpsEvent WHERE Date BETWEEN :1 AND :2 ORDER BY Date DESC",
                start,
                end
            );

            return list.Select(x => new CommandItem
            {
                Text = $"{x.Date.ToLocalTime()}",
                Detail = $"{x.Latitude} / {x.Longitude}",
                PrimaryCommand = ReactiveCommand.CreateFromTask(async () =>
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

                    await this.Dialogs.Alert(msg);
                })
            });
        }
    }
}
