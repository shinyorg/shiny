using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Shiny;
using Shiny.BluetoothLE;


namespace Samples.BluetoothLE
{
    public class PeripheralViewModel : ViewModel
    {
        readonly IDialogs dialogs;
        readonly IBleManager centralManager;
        IPeripheral peripheral;


        public PeripheralViewModel(IBleManager centralManager, IDialogs dialogs)
        {
            this.centralManager = centralManager;
            this.dialogs = dialogs;

            this.SelectCharacteristic = ReactiveCommand.Create<GattCharacteristicViewModel>(x => x.Select());

            this.ConnectionToggle = ReactiveCommand.Create(() =>
            {
                // don't cleanup connection - force user to d/c
                if (this.peripheral.Status == ConnectionState.Disconnected)
                {
                    this.peripheral.Connect();
                }
                else
                {
                    this.peripheral.CancelConnection();
                }
            });

            this.PairToDevice = ReactiveCommand.CreateFromTask(async () =>
            {
                var pair = this.peripheral as ICanPairPeripherals;

                if (pair == null)
                {
                    await dialogs.Alert("Pairing is not supported on this platform");
                }
                else if (pair.PairingStatus == PairingState.Paired)
                {
                    await dialogs.Snackbar("Peripheral is already paired");
                }
                else
                {
                    pair
                        .PairingRequest()
                        .Subscribe(x =>
                        {
                            var txt = x ? "Peripheral Paired Successfully" : "Peripheral Pairing Failed";
                            dialogs.Snackbar(txt);
                            this.RaisePropertyChanged(nameof(this.PairingText));
                        });
                }
            });

            this.RequestMtu = ReactiveCommand.CreateFromTask(
                async x =>
                {
                    var mtu = this.peripheral as ICanRequestMtu;
                    if (mtu == null)
                    {
                        await dialogs.Alert("MTU requests are not supported on this platform");
                    }
                    else
                    {
                        var result = await dialogs.Input("MTU Request", "Range 20-512");
                        if (!result.IsEmpty())
                        {
                            var actual = await mtu.RequestMtu(Int32.Parse(result));
                            await dialogs.Snackbar("MTU Changed to " + actual);
                        }
                    }
                },
                this.WhenAny(
                    x => x.ConnectText,
                    x => x.GetValue().Equals("Disconnect")
                )
            );
        }


        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);

            this.peripheral = parameters.GetValue<IPeripheral>("Peripheral");
            this.Name = this.peripheral.Name;
            this.Uuid = this.peripheral.Uuid;

            this.IsMtuVisible = this.peripheral.IsMtuRequestsAvailable();
            this.IsPairingVisible = this.peripheral.IsPairingRequestsAvailable();

            this.PairingText = this.peripheral.TryGetPairingStatus() == PairingState.Paired ? "Peripheral Paired" : "Pair Peripheral";


            this.peripheral
                .ReadRssiContinuously(TimeSpan.FromSeconds(3))
                .SubOnMainThread(x => this.Rssi = x)
                .DisposeWith(this.DeactivateWith);

            this.peripheral
                .WhenStatusChanged()
                .SubOnMainThread(status =>
                {
                    switch (status)
                    {
                        case ConnectionState.Connecting:
                            this.ConnectText = "Cancel Connection";
                            break;

                        case ConnectionState.Connected:
                            this.ConnectText = "Disconnect";
                            break;

                        case ConnectionState.Disconnected:
                            this.ConnectText = "Connect";
                            try
                            {
                                this.GattCharacteristics.Clear();
                            }
                            catch (Exception ex)
                            {
                                // eat this for now
                                Console.WriteLine(ex);
                            }

                            break;
                    }
                })
                .DisposeWith(this.DeactivateWith);

            this.peripheral
                .WhenAnyCharacteristicDiscovered()
                .SubOnMainThread(
                    chs =>
                    {
                        var service = this.GattCharacteristics.FirstOrDefault(x => x.ShortName.Equals(chs.Service.Uuid.ToString()));
                        if (service == null)
                        {
                            service = new Group<GattCharacteristicViewModel>(
                                chs.Service.Uuid.ToString(),
                                chs.Service.Uuid.ToString()
                            );
                            this.GattCharacteristics.Add(service);
                        }

                        service.Add(new GattCharacteristicViewModel(chs, this.dialogs));
                    },
                    ex => this.dialogs.Snackbar(ex.ToString())
                )
                .DisposeWith(this.DeactivateWith);
        }


        public ICommand ConnectionToggle { get; }
        public ICommand PairToDevice { get; }
        public ICommand RequestMtu { get; }
        public ICommand SelectCharacteristic { get; }

        [Reactive] public string Name { get; private set; }
        [Reactive] public string Uuid { get; private set; }
        [Reactive] public string PairingText { get; private set; }
        public ObservableCollection<Group<GattCharacteristicViewModel>> GattCharacteristics { get; } = new ObservableCollection<Group<GattCharacteristicViewModel>>();

        [Reactive] public string ConnectText { get; private set; } = "Connect";
        [Reactive] public int Rssi { get; private set; }

        [Reactive] public bool IsMtuVisible { get; private set; }
        [Reactive] public bool IsPairingVisible { get; private set; }
    }
}
