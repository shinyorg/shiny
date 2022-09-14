using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Shiny;
using Shiny.Locations;


namespace Sample
{
    public class ListViewModel : SampleViewModel
    {
        public ListViewModel()
        {
            var geofenceManager = ShinyHost.Resolve<IGeofenceManager>();

            this.Create = this.NavigateCommand<CreatePage>();

            this.DropAllFences = this.ConfirmCommand(
                "Are you sure you wish to drop all geofences?",
                async () =>
                {
                    await geofenceManager.StopAllMonitoring();
                    this.Load!.Execute(null);
                }
            );

            this.Load = this.LoadingCommand(async () =>
            {
                var geofences = await geofenceManager.GetMonitorRegions();

                this.Geofences = geofences
                    .Select(region => new GeofenceRegionViewModel
                    (
                        region,
                        this.ConfirmCommand
                        (
                            "Are you sure you wish to remove geofence - " + region.Identifier,
                            async () =>
                            {
                                await geofenceManager.StopMonitoring(region.Identifier);
                                this.Load!.Execute(null);
                            }
                        ),
                        this.LoadingCommand(async () =>
                        {
                            var status = await geofenceManager.RequestState(region);
                            await this.Alert($"{region.Identifier} status is {status}");
                        })
                    ))
                    .ToList();

                this.RaisePropertyChanged(nameof(this.Geofences));
            });
        }


        public ICommand Create { get; }
        public ICommand Load { get; }
        public ICommand DropAllFences { get; }
        public IList<GeofenceRegionViewModel> Geofences { get; private set; }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.Load.Execute(null);
        }
    }
}
