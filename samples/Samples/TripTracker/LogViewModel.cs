using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using Shiny;
using Shiny.TripTracker;
using Samples.Infrastructure;
using Humanizer;

namespace Samples.TripTracker
{
    public class LogViewModel : AbstractLogViewModel<CommandItem>
    {
        readonly ITripTrackerManager manager;
        public LogViewModel(ITripTrackerManager manager, IDialogs dialogs) : base(dialogs)
            => this.manager = manager;


        protected override Task ClearLogs() => this.manager.Purge();


        protected override async Task<IEnumerable<CommandItem>> LoadLogs()
        {
            var trips = await this.manager.GetAllTrips();

            return trips.Select(x =>
            {
                
                var text = String.Empty;
                var details = String.Empty;

                if (x.DateFinished == null)
                {
                    text = $"{x.Type.ToString().ToUpper()} In-Progress";
                    details = x.DateStarted.LocalDateTime.ToString("g");
                }
                else
                {
                    text = $"{x.Type.ToString().ToUpper()} Finished at {x.DateFinished.Value.LocalDateTime:g}";

                    var time = (x.DateFinished - x.DateStarted).Value.Humanize();
                    var avgSpeed = Math.Round(Distance.FromMeters(x.AverageSpeedMetersPerHour).TotalKilometers, 0);
                    var km = Math.Round(Distance.FromMeters(x.TotalDistanceMeters).TotalKilometers, 1);

                    details = $"Distance: {km} km - Time Taken: {time} - Avg. Speed (km/h): {avgSpeed}";
                }

                return new CommandItem
                {
                    Text = text,
                    Detail = details,
                    PrimaryCommand = ReactiveCommand.CreateFromTask(async () =>
                    {
                        var email = await this.Dialogs.Input("Do you wish to email this trip?  If so, enter and ok it!");
                        if (email == null)
                            return;

                        var sb = new StringBuilder();
                        var checkins = await this.manager.GetCheckinsByTrip(x.Id);
                        sb
                            .AppendLine($"Trip: {x.Id}")
                            .AppendLine()
                            .AppendLine($"Started: {x.DateStarted.LocalDateTime:g}")
                            .AppendLine($"Start Location: {x.StartLatitude} / {x.StartLongitude}")
                            .AppendLine();

                        if (x.DateFinished != null)
                        {
                            sb
                                .AppendLine($"Finished: {x.DateFinished.Value.LocalDateTime:g}")
                                .AppendLine($"Finish Location: {x.EndLatitude} / {x.EndLongitude}")
                                .AppendLine();
                        }

                        var meters = Math.Round(x.TotalDistanceMeters);
                        sb
                            .AppendLine($"Total Distance (meters): {meters}")
                            .AppendLine($"GPS Pings: {checkins.Count()}")
                            .AppendLine()
                            .AppendLine("Lat,Long,Speed,Direction,Ticks");

                        foreach (var checkin in checkins)
                            sb.AppendLine($"{checkin.Latitude},{checkin.Longitude},{checkin.Speed},{checkin.Direction},{checkin.DateCreated.Ticks}");

                        await Xamarin.Essentials.Email.ComposeAsync("Shiny Trip", sb.ToString(), email);
                    })
                };
            });
        }
    }
}
