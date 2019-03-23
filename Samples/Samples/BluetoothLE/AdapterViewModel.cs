using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Shiny;
using Acr.UserDialogs;
using Shiny.BluetoothLE.Central;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples.BluetoothLE
{
    public class AdapterViewModel : ViewModel
    {
        IDisposable scan;


        public AdapterViewModel(ICentralManager central,
                                INavigationService navigationService,
                                IUserDialogs dialogs)
        {
            this.SelectPeripheral = ReactiveCommand.CreateFromTask<ScanResultViewModel>(
                x => navigationService.Navigate(
                    "Peripheral",
                    ("Peripheral", x.Peripheral)
                )
            );

            this.OpenSettings = ReactiveCommand.Create(() =>
            {
                if (central.Features.HasFlag(BleFeatures.OpenSettings))
                {
                    central.OpenSettings();
                }
                else
                {
                    dialogs.Alert("Cannot open bluetooth settings");
                }
            });

            this.ToggleAdapterState = ReactiveCommand.Create(
                () =>
                {
                    if (central.CanControlAdapterState())
                    {
                        var poweredOn = central.Status == AccessState.Available;
                        central.SetAdapterState(!poweredOn);
                    }
                    else
                    {
                        dialogs.Alert("Cannot change bluetooth adapter state");
                    }
                }
            );

            this.ScanToggle = ReactiveCommand.Create(
                () =>
                {
                    if (this.IsScanning)
                    {
                        this.scan?.Dispose();
                    }
                    else
                    {
                        this.Peripherals.Clear();

                        this.scan = central
                            .Scan()
                            .Buffer(TimeSpan.FromSeconds(1))
                            .Synchronize()
                            .SubOnMainThread(
                                results =>
                                {
                                    var list = new List<ScanResultViewModel>();
                                    foreach (var result in results)
                                    {
                                        var dev = this.Peripherals.FirstOrDefault(x => x.Uuid.Equals(result.Peripheral.Uuid));

                                        if (dev != null)
                                        {
                                            dev.TrySet(result);
                                        }
                                        else
                                        {
                                            dev = new ScanResultViewModel();
                                            dev.TrySet(result);
                                            list.Add(dev);
                                        }
                                    }
                                    if (list.Any())
                                        this.Peripherals.AddRange(list);
                                },
                                ex => dialogs.Alert(ex.ToString(), "ERROR")
                            )
                            .DisposeWith(this.DeactivateWith);
                    }
                }
            );
        }


        public override void OnDisappearing()
        {
            base.OnDisappearing();
            this.IsScanning = false;
        }

        public ICommand ScanToggle { get; }
        public ICommand OpenSettings { get; }
        public ICommand ToggleAdapterState { get; }
        public ICommand SelectPeripheral { get; }
        public ObservableList<ScanResultViewModel> Peripherals { get; } = new ObservableList<ScanResultViewModel>();
        [Reactive] public bool IsScanning { get; private set; }
    }
}