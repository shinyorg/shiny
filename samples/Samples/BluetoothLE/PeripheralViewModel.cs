using System;
using System.Linq;
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
        IPeripheral peripheral;


        public PeripheralViewModel(IDialogs dialogs)
        {
            this.dialogs = dialogs;

            this.SelectCharacteristic = ReactiveCommand.Create<GattCharacteristicViewModel>(x => x.Select());

            this.ConnectionToggle = ReactiveCommand.Create(() =>
            {
                // don't cleanup connection - force user to d/c
                if (this.peripheral.Status == ConnectionState.Connected || this.peripheral.Status == ConnectionState.Connecting)
                {
                    this.peripheral.CancelConnection();
                }
                else
                {
                    this.peripheral.Connect();
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
                    var result = await pair.PairingRequest(); //.Timeout(TimeSpan.FromSeconds(10));
                    await dialogs.Snackbar(result ? "Peripheral Paired Successfully" : "Peripheral Pairing Failed");
                    this.RaisePropertyChanged(nameof(this.PairingText));
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

            this.PairingText = this.peripheral.TryGetPairingStatus() == PairingState.Paired
                ? "Peripheral Paired"
                : "Pair Peripheral";

            //this.peripheral
            //    .WhenConnected()
            //    .Select(x => x.ReadRssiContinuously(TimeSpan.FromSeconds(3)))
            //    .Switch()
            //    .SubOnMainThread(x => this.Rssi = x)
            //    .DisposedBy(this.DeactivateWith);

            this.peripheral
                .WhenStatusChanged()
                .Select(x => x switch
                {
                    ConnectionState.Connecting => "Cancel Connection",
                    ConnectionState.Connected => "Disconnect",
                    ConnectionState.Disconnected => "Connect",
                    ConnectionState.Disconnecting => "Disconnecting..."
                })
                .SubOnMainThread(x =>
                {
                    this.ConnectText = x;
                })
                .DisposedBy(this.DeactivateWith);

            this.peripheral
                .WhenConnected()
                .Do(x => this.GattCharacteristics.Clear())
                .SelectMany(x => x.GetAllCharacteristics())
                .Select(x => x.Select(y => new GattCharacteristicViewModel(y, this.dialogs)))
                .SubOnMainThread(
                    list =>
                    {
                        var chars = list
                            .GroupBy(x => x.ServiceUuid)
                            .Select(x =>
                            {
                                var group = new Group<GattCharacteristicViewModel>(x.Key, x.Key);
                                group.AddRange(x.ToList());
                                return group;
                            })
                            .ToList();

                        this.GattCharacteristics.AddRange(chars);
                    },
                    ex => this.dialogs.Snackbar(ex.ToString())
                )
                .DisposedBy(this.DeactivateWith);
        }


        public ICommand ConnectionToggle { get; }
        public ICommand PairToDevice { get; }
        public ICommand RequestMtu { get; }
        public ICommand SelectCharacteristic { get; }

        [Reactive] public string Name { get; private set; }
        [Reactive] public string Uuid { get; private set; }
        [Reactive] public string PairingText { get; private set; }
        public ObservableList<Group<GattCharacteristicViewModel>> GattCharacteristics { get; } = new ObservableList<Group<GattCharacteristicViewModel>>();

        [Reactive] public string ConnectText { get; private set; } = "Connect";
        [Reactive] public int Rssi { get; private set; }

        [Reactive] public bool IsMtuVisible { get; private set; }
        [Reactive] public bool IsPairingVisible { get; private set; }
    }
}
