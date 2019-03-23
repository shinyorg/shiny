using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using Acr.UserDialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Models;
using Humanizer;


namespace Samples.Gps
{
    public class LogsViewModel : ViewModel
    {
        public LogsViewModel(SampleSqliteConnection conn, IUserDialogs dialogs)
        {
            this.ClearLogs = ReactiveCommand.CreateFromTask(async () =>
            {
                var confirm = await dialogs.ConfirmAsync("DO you want to delete all GPS logs?", "Confirm", "Yes", "No");
                if (confirm)
                {
                    await conn.DeleteAllAsync<GpsEvent>();
                    this.LoadLogs.Execute();
                }
            });

            this.LoadLogs = ReactiveCommand.CreateFromTask(async () =>
            {
                var list = await conn
                    .GpsEvents
                    .OrderByDescending(x => x.Date)
                    .ToListAsync();

                this.Logs = list
                    .Select(x => new CommandItem
                    {
                        Data = x,
                        Text = $"{x.Date}",
                        Detail = $"{x.Latitude} / {x.Longitude}"
                    })
                    .ToList();
            });
            this.BindBusyCommand(this.LoadLogs);

            this.ShowLog = ReactiveCommand.Create<CommandItem>(item =>
            {
                var e = (GpsEvent)item.Data;
                var msg = new StringBuilder()
                    .AppendLine("Lat: " + e.Latitude)
                    .AppendLine("Lng: " + e.Longitude)
                    .AppendLine("Alt: " + e.Altitude)
                    .AppendLine("Position Accuracy: " + e.PositionAccuracy)
                    .AppendLine("Heading: " + e.Heading.ToHeading())
                    .AppendLine("Heading Accuracy: " + e.HeadingAccuracy)
                    .AppendLine("Speed (m/s) " + e.Speed)
                    .ToString();

                dialogs.Alert(msg);
            });
        }


        [Reactive] public IList<CommandItem> Logs { get; private set; }
        public ReactiveCommand<CommandItem, Unit> ShowLog { get; }
        public ReactiveCommand<Unit, Unit> LoadLogs { get; }
        public ReactiveCommand<Unit, Unit> ClearLogs { get; }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.LoadLogs.Execute();
        }
    }
}
