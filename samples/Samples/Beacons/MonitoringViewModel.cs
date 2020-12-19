using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Shiny.Beacons;


namespace Samples.Beacons
{
    public class MonitoringViewModel : ViewModel
    {
        public MonitoringViewModel(INavigationService navigator,
                                   IDialogs dialogs,
                                   IBeaconMonitoringManager? beaconManager = null)
        {
            this.Add = navigator.NavigateCommand("CreateBeacon");
            this.Load = ReactiveCommand.CreateFromTask(async () =>
            {
                if (beaconManager == null)
                {
                    await dialogs.Alert("Beacon monitoring is not supported on this platform");
                    return;
                }
                var regions = await beaconManager.GetMonitoredRegions();

                this.Regions = regions
                    .Select(x => new CommandItem
                    {
                        Text = $"{x.Identifier}",
                        Detail = $"{x.Uuid}/{x.Major ?? 0}/{x.Minor ?? 0}",
                        PrimaryCommand = ReactiveCommand.CreateFromTask(async () =>
                        {
                            await beaconManager.StopMonitoring(x.Identifier);
                            this.Load.Execute(null);
                        })
                    })
                    .ToList();
            });

            this.StopAllMonitoring = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    var result = await dialogs.Confirm("Are you sure you wish to stop all monitoring");
                    if (result)
                    {
                        await beaconManager.StopAllMonitoring();
                        this.Load.Execute(null);
                    }
                },
                Observable.Return(beaconManager != null)
            );
        }


        public ICommand Load { get; }
        public ICommand Add { get; }
        public ICommand StopAllMonitoring { get; }
        [Reactive] public IList<CommandItem> Regions { get; private set; }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.Load.Execute(null);
        }
    }
}