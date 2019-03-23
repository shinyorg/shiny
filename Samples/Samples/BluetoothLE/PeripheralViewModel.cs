using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Acr.UserDialogs;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Central;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples.BluetoothLE
{
    public class PeripheralViewModel : ViewModel
    {
        IPeripheral peripheral;


        public PeripheralViewModel(ICentralManager centralManager, IUserDialogs dialogs)
        {
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

            this.PairToDevice = ReactiveCommand.Create(() =>
            {
                if (!centralManager.Features.HasFlag(BleFeatures.PairingRequests))
                {
                    dialogs.Toast("Pairing is not supported on this platform");
                }
                else if (this.peripheral.PairingStatus == PairingState.Paired)
                {
                    dialogs.Toast("Peripheral is already paired");
                }
                else
                {
                    this.peripheral
                        .PairingRequest()
                        .Subscribe(x =>
                        {
                            var txt = x ? "Peripheral Paired Successfully" : "Peripheral Pairing Failed";
                            dialogs.Toast(txt);
                            this.RaisePropertyChanged(nameof(this.PairingText));
                        });
                }
            });

            this.RequestMtu = ReactiveCommand.CreateFromTask(
                async x =>
                {
                    if (!centralManager.Features.HasFlag(BleFeatures.MtuRequests))
                    {
                        dialogs.Alert("MTU Request not supported on this platform");
                    }
                    else
                    {
                        var result = await dialogs.PromptAsync(new PromptConfig()
                            .SetTitle("MTU Request")
                            .SetMessage("Range 20-512")
                            .SetInputMode(InputType.Number)
                            .SetOnTextChanged(args =>
                            {
                                var len = args.Value?.Length ?? 0;
                                if (len > 0)
                                {
                                    if (len > 3)
                                    {
                                        args.Value = args.Value.Substring(0, 3);
                                    }
                                    else
                                    {
                                        var value = Int32.Parse(args.Value);
                                        args.IsValid = value >= 20 && value <= 512;
                                    }
                                }
                            })
                        );
                        if (result.Ok)
                        {
                            var actual = await this.peripheral.RequestMtu(Int32.Parse(result.Text));
                            dialogs.Toast("MTU Changed to " + actual);
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
            this.PairingText = this.peripheral.PairingStatus == PairingState.Paired ? "Peripheral Paired" : "Pair Peripheral";

            this.peripheral
                .WhenReadRssiContinuously(TimeSpan.FromSeconds(3))
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
                                Console.WriteLine(ex);
                            }

                            break;
                    }
                })
                .DisposeWith(this.DeactivateWith);

            this.peripheral
                .WhenAnyCharacteristicDiscovered()
                .SubOnMainThread(chs =>
                {
                    try
                    {
                        var service = this.GattCharacteristics.FirstOrDefault(x => x.ShortName.Equals(chs.Service.Uuid.ToString()));
                        if (service == null)
                        {
                            service = new Group<GattCharacteristicViewModel>(
                                $"{chs.Service.Description} ({chs.Service.Uuid})",
                                chs.Service.Uuid.ToString()
                            );
                            this.GattCharacteristics.Add(service);
                        }

                        service.Add(new GattCharacteristicViewModel(chs));
                    }
                    catch (Exception ex)
                    {
                        // eat it
                        Console.WriteLine(ex);
                    }
                })
                .DisposeWith(this.DeactivateWith);
        }


        public ICommand ConnectionToggle { get; }
        public ICommand PairToDevice { get; }
        public ICommand RequestMtu { get; }
        public ICommand SelectCharacteristic { get; }

        [Reactive] public string Name { get; private set; }
        [Reactive] public Guid Uuid { get; private set; }
        [Reactive] public string PairingText { get; private set; }
        public ObservableCollection<Group<GattCharacteristicViewModel>> GattCharacteristics { get; } = new ObservableCollection<Group<GattCharacteristicViewModel>>();

        [Reactive] public string ConnectText { get; private set; } = "Connect";
        [Reactive] public int Rssi { get; private set; }
    }
}
