using System;
using System.Collections.Generic;
using System.Windows.Input;
using ReactiveUI;
using Shiny.Locations.Sync;


namespace Samples.LocationSync
{
    public class PendingQueueViewModel : ViewModel
    {
        public PendingQueueViewModel(ILocationSyncManager syncManager)
        {
            this.Load = ReactiveCommand.CreateFromTask(async () =>
            {
                this.Items.Clear();

                var gps = await syncManager.GetPendingGpsEvents();
                foreach (var item in gps)
                {
                    this.Items.Add(new CommandItem
                    {
                        Text = $"GPS Lat: {item.Latitude} - Long: {item.Longitude}",
                        Detail = item.DateCreated.LocalDateTime.ToLongDateString()
                    });
                }

                var geofences = await syncManager.GetPendingGeofenceEvents();
                foreach (var geofence in geofences)
                {
                    this.Items.Add(new CommandItem
                    {
                        Text = $"Geofence {geofence.Identifier} (Entered: {geofence.Entered})",
                        Detail = geofence.DateCreated.LocalDateTime.ToLongDateString()
                    });
                }
                this.RaisePropertyChanged(nameof(this.Items));
            });
            this.BindBusyCommand(this.Load);
        }


        public ICommand Load { get; }
        public IList<CommandItem> Items { get; } = new List<CommandItem>();


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.Load.Execute(null);
        }
    }
}
