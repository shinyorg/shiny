using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Shiny.Beacons;
using Acr.UserDialogs;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples.Beacons
{
    public class MonitoringViewModel : ViewModel
    {
        readonly IBeaconManager beaconManager;


        public MonitoringViewModel(INavigationService navigationService,
                                   IUserDialogs dialogs,
                                   IBeaconManager beaconManager)
        {
            this.beaconManager = beaconManager;

            this.Add = ReactiveCommand.CreateFromTask(() => navigationService.Navigate(
                nameof(CreatePage),
                ("Monitoring", true)
            ));

            this.StopAllMonitoring = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await dialogs.ConfirmAsync("Are you sure you wish to stop all monitoring");
                if (result)
                {
                    await this.beaconManager.StopAllMonitoring();
                    await this.LoadData();
                }
            });
        }


        public ICommand Add { get; }
        public ICommand StopAllMonitoring { get; }
        [Reactive] public IList<CommandItem> Regions { get; private set; }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.LoadData();
        }


        public override void OnNavigatingTo(INavigationParameters parameters)
        {
            var newRegion = parameters.GetValue<BeaconRegion>(nameof(BeaconRegion));
            if (newRegion != null)
                this.beaconManager.StartMonitoring(newRegion);
        }


        async Task LoadData()
        {
            var regions = await this.beaconManager.GetMonitoredRegions();

            this.Regions = regions
                .Select(x => new CommandItem
                {
                    Text = $"{x.Identifier}",
                    Detail = $"{x.Uuid}/{x.Major ?? 0}/{x.Minor ?? 0}",
                    PrimaryCommand = ReactiveCommand.CreateFromTask(async () =>
                    {
                        await this.beaconManager.StopMonitoring(x);
                        await this.LoadData();
                    })
                })
                .ToList();
        }
    }
}