using System;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using Shiny.BluetoothLE.Peripherals;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples.BlePeripherals
{
    public class MainViewModel : ViewModel
    {
        static readonly Guid ServiceUuid = Guid.Parse("A495FF20-C5B1-4B44-B512-1370F02D74DE");
        static readonly Guid NotifyCharacteristicUuid = Guid.Parse("A495FF21-C5B1-4B44-B512-1370F02D74DE");
        static readonly Guid ReadWriteCharacteristicUuid = Guid.Parse("A495FF21-C5B1-4B44-B512-1370F02D74DE");

        IGattCharacteristic notifications;
        IDisposable timer;


        public MainViewModel(IPeripheralManager peripheral)
        {
            this.ToggleServer = ReactiveCommand.CreateFromTask(async _ =>
            {
                if (peripheral.IsAdvertising)
                {
                    this.timer?.Dispose();
                    this.IsRunning = false;
                    peripheral.ClearServices();
                    peripheral.StopAdvertising();
                    this.Write("GATT Server Stopped");
                }
                else
                {
                    await peripheral.AddService
                    (
                        ServiceUuid,
                        true,
                        sb =>
                        {
                            this.notifications = sb.AddCharacteristic
                            (
                                NotifyCharacteristicUuid,
                                cb => cb.SetNotification(cs =>
                                {
                                    var @event = cs.IsSubscribing ? "Subscribed" : "Unsubcribed";
                                    this.Write($"Device {cs.Peripheral.Uuid} {@event}");
                                    //this.Write($"Charcteristic Subcribers: {characteristic.SubscribedDevices.Count}");
                                })
                            );

                            sb.AddCharacteristic
                            (
                                ReadWriteCharacteristicUuid,
                                cb => cb
                                    .SetRead(req =>
                                    {
                                        var bytes = Encoding.UTF8.GetBytes(this.CharacteristicValue ?? "Test");
                                        this.Write($"Characteristic Read Received - {bytes}");
                                        return ReadResult.Success(bytes);
                                    })
                                    .SetWrite(req =>
                                    {
                                        var write = Encoding.UTF8.GetString(req.Data, 0, req.Data.Length);
                                        this.Write($"Characteristic Write Received - {write}");
                                        return GattState.Success;
                                    })
                            );
                        }
                    );
                    await peripheral.StartAdvertising(new AdvertisementData
                    {
                        LocalName = "My GATT"
                    });
                    this.Write("GATT Server Started");

                    var count = 0;
                    this.timer = Observable
                        .Timer(TimeSpan.FromSeconds(10))
                        // TODO: only when characteristic has subscribers
                        .Subscribe(async x =>
                        {
                            count++;
                            //await this.notifications.Notify(Encoding.UTF8.GetBytes(count.ToString()));
                        });

                    this.IsRunning = true;
                }
            });

            this.Clear = ReactiveCommand.Create(() => this.Output = String.Empty);
        }


        [Reactive] public bool IsRunning { get; private set; }
        [Reactive] public string CharacteristicValue { get; set; }
        [Reactive] public string Output { get; private set; }
        public ICommand ToggleServer { get; }
        public ICommand Clear { get; }

        void Write(string msg) => this.Output += msg + Environment.NewLine + Environment.NewLine;
    }
}