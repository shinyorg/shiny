using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using Prism.Navigation;
using Shiny.Locations;
using Samples.Models;
using Samples.Infrastructure;


namespace Samples.Geofences
{
    public class ListViewModel : ViewModel
    {
        readonly IGeofenceManager geofenceManager;
        readonly IDialogs dialogs;


        public ListViewModel(INavigationService navigator,
                             IGeofenceManager geofenceManager,
                             IDialogs dialogs)
        {
            this.geofenceManager = geofenceManager;
            this.dialogs = dialogs;

            this.Create = navigator.NavigateCommand("CreateGeofence");
            this.DropAllFences = ReactiveCommand.CreateFromTask(
                async _ =>
                {
                    var confirm = await this.dialogs.Confirm("Are you sure you wish to drop all geofences?");
                    if (confirm)
                    {
                        await this.geofenceManager.StopAllMonitoring();
                        await this.LoadRegions();
                    }
                },
                this.WhenAny(
                    x => x.HasGeofences,
                    x => x.GetValue()
                )
            );
        }


        public ICommand Create { get; }
        public ICommand DropAllFences { get; }

        public bool HasGeofences => this.Geofences.Any();
        public IList<GeofenceRegionViewModel> Geofences { get; private set; } = new List<GeofenceRegionViewModel>();
        public bool HasEvents => this.Events.Any();
        public IList<GeofenceEvent> Events { get; private set; } = new List<GeofenceEvent>();


        public override async void OnAppearing()
        {
            base.OnAppearing();
            await this.LoadRegions();
        }


        async Task LoadRegions()
        {
            var geofences = await this.geofenceManager.GetMonitorRegions();

            this.Geofences = geofences
                .Select(region => new GeofenceRegionViewModel
                {
                    Region = region,
                    Remove = ReactiveCommand.CreateFromTask(async _ =>
                    {
                        var confirm = await this.dialogs.Confirm("Are you sure you wish to remove geofence - " + region.Identifier);
                        if (confirm)
                        {
                            await this.geofenceManager.StopMonitoring(region.Identifier);
                            await this.LoadRegions();
                        }
                    }),
                    RequestCurrentState = ReactiveCommand.CreateFromTask(async _ =>
                    {
                        GeofenceState? status = null;
                        using (var cancelSrc = new CancellationTokenSource())
                        {
                            //using (this.dialogs.Loading("Requesting State for " + region.Identifier, cancelSrc.Cancel))
                                status = await this.geofenceManager.RequestState(region, cancelSrc.Token);
                        }

                        if (status != null)
                        {
                            await Task.Delay(2000);
                            await this.dialogs.Alert($"{region.Identifier} status is {status}");
                        }
                    })
                })
                .ToList();

            this.RaisePropertyChanged(nameof(this.Geofences));
            this.RaisePropertyChanged(nameof(this.HasGeofences));
        }
    }
}
