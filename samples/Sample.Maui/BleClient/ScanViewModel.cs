using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Shiny;
using Shiny.BluetoothLE;
using Xamarin.Forms;


namespace Sample
{
    public class ScanViewModel : SampleViewModel
    {
        readonly IBleManager bleManager;
        IDisposable? scanSub;


        public ScanViewModel()
        {
            var bleManager = ShinyHost.Resolve<IBleManager>();
            this.IsScanning = bleManager?.IsScanning ?? false;
            this.CanControlAdapterState = bleManager?.CanControlAdapterState() ?? false;

            this.WhenAnyProperty(x => x.SelectedPeripheral)
                .Skip(1)
                .Where(x => x != null)
                .Subscribe(async x =>
                {
                    this.SelectedPeripheral = null;
                    this.StopScan();
                    await this.Navigation.PushAsync(new PeripheralPage
                    {
                        BindingContext = new PeripheralViewModel(x.Peripheral)
                    });
                });

            this.NavToTest = this.NavigateCommand<TestPage>();

            this.ToggleAdapterState = new Command(
                async () =>
                {
                    if (bleManager == null)
                    {
                        await this.Alert("Platform Not Supported");
                    }
                    else
                    {
                        var status = await bleManager.RequestAccess();
                        if (status == AccessState.Available)
                        {
                            await bleManager.TrySetAdapterState(false);
                        }
                        else
                        {
                            await bleManager.TrySetAdapterState(true);
                        }
                    }
                }
            );

            this.ScanToggle = new Command(
                async () =>
                {
                    if (bleManager == null)
                    {
                        await this.Alert("Platform Not Supported");
                        return;
                    }
                    if (this.IsScanning)
                    {
                        this.StopScan();
                    }
                    else
                    {
                        this.Peripherals.Clear();
                        this.IsScanning = true;

                        this.scanSub = bleManager
                            .Scan()
                            .Buffer(TimeSpan.FromSeconds(1))
                            .Where(x => x?.Any() ?? false)
                            .SubOnMainThread(
                                results =>
                                {
                                    var list = new List<PeripheralItemViewModel>();
                                    foreach (var result in results)
                                    {
                                        var peripheral = this.Peripherals.FirstOrDefault(x => x.Equals(result.Peripheral));
                                        if (peripheral == null)
                                            peripheral = list.FirstOrDefault(x => x.Equals(result.Peripheral));

                                        if (peripheral != null)
                                        {
                                            peripheral.Update(result);
                                        }
                                        else
                                        {
                                            peripheral = new PeripheralItemViewModel(result.Peripheral);
                                            peripheral.Update(result);
                                            list.Add(peripheral);
                                        }
                                    }
                                    if (list.Any())
                                    {
                                        // XF is not able to deal with an observablelist/addrange properly
                                        foreach (var item in list)
                                            this.Peripherals.Add(item);
                                    }
                                },
                                ex => this.Alert(ex.ToString(), "ERROR")
                            );
                    }
                }
            );
        }


        public ICommand NavToTest { get; }
        public ICommand ScanToggle { get; }
        public ICommand ToggleAdapterState { get; }
        public bool CanControlAdapterState { get; }
        public ObservableCollection<PeripheralItemViewModel> Peripherals { get; } = new ObservableCollection<PeripheralItemViewModel>();


        PeripheralItemViewModel? selected;
        public PeripheralItemViewModel? SelectedPeripheral
        {
            get => this.selected;
            set
            {
                this.selected = value;
                this.RaisePropertyChanged();
            }
        }


        bool scanning;
        public bool IsScanning
        {
            get => this.scanning;
            private set => this.Set(ref this.scanning, value);
        }


        void StopScan()
        {
            this.scanSub?.Dispose();
            this.scanSub = null;
            this.IsScanning = false;
        }
    }
}