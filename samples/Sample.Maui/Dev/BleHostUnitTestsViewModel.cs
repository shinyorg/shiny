using System.Text;
using Shiny.BluetoothLE.Hosting;
using Shiny.Tests.BluetoothLE;

namespace Sample.Dev;


public record BleLog(
    string Timestamp,
    string Event,
    string Data
);

public class BleHostUnitTestsViewModel : ViewModel
{
    public BleHostUnitTestsViewModel(BaseServices services, IBleHostingManager hostingManager) : base(services)
    {
        this.StartServer = ReactiveCommand.CreateFromTask(
            async () =>
            {
                await hostingManager.AddService(
                    BleConfiguration.ServiceUuid,
                    true,
                    sb =>
                    {
                        byte[]? currentData = null;

                        var notifier = sb.AddCharacteristic(BleConfiguration.NotifyCharacteristicUuid, x => x.SetNotification(sub =>
                        {
                            var smsg = sub.IsSubscribing ? "Subscribed" : "UnSubscribed";
                            this.Log($"{sub.Peripheral.Uuid} {smsg} to Characteristic");

                            return Task.CompletedTask;
                        }));


                        var notifier2 = sb.AddCharacteristic(BleConfiguration.Notify2CharacteristicUuid, x => x.SetNotification(sub =>
                        {
                            var smsg = sub.IsSubscribing ? "Subscribed" : "UnSubscribed";
                            this.Log($"{sub.Peripheral.Uuid} {smsg} to Characteristic Notifier 2");

                            return Task.CompletedTask;
                        }));


                        sb.AddCharacteristic(BleConfiguration.ReadCharacteristicUuid, x => x.SetRead(request =>
                        {
                            var data = currentData ?? new byte[] { 0x0 };
                            this.Log($"{request.Peripheral.Uuid} Read Characteristic", data);

                            return Task.FromResult(GattResult.Success(data));
                        }));

                        sb.AddCharacteristic(BleConfiguration.WriteCharacteristicUuid, cb => cb.SetWrite(async request =>
                        {
                            currentData = request.Data;
                            this.Log($"{request.Peripheral.Uuid} Wrote to Characteristic", request.Data);

                            if (notifier.SubscribedCentrals.Count == 0)
                            {
                                this.Log("No subscribers on 1");
                            }
                            else
                            {
                                await notifier.Notify(request.Data);
                                this.Log("Notification Broadcasted to subscribers");
                            }
                            if (notifier2.SubscribedCentrals.Count == 0)
                            {
                                this.Log("No subscribers on 2");
                            }
                            else
                            { 
                                await notifier2.Notify(request.Data);
                                this.Log("Notification Broadcasted to subscribers");
                            }
                        }, WriteOptions.Write | WriteOptions.WriteWithoutResponse));
                    }
                );
                await hostingManager.AddService(
                    BleConfiguration.SecondaryServiceUuid,
                    true,
                    sb =>
                    {
                        sb.AddCharacteristic(BleConfiguration.SecondaryCharacteristicUuid1, x => x.SetRead(async request =>
                        {
                            this.Log("Secondary Read - CH1");
                            return GattResult.Success(new byte[] { 0x01 });
                        }));

                        sb.AddCharacteristic(BleConfiguration.SecondaryCharacteristicUuid2, x => x.SetRead(async request =>
                        {
                            this.Log("Secondary Read - CH2");
                            return GattResult.Success(new byte[] { 0x01 });
                        }));
                    }
                );
                await hostingManager.StartAdvertising(new(ServiceUuids: new[]
                {
                    BleConfiguration.ServiceUuid
                }));
                this.IsHostOnline = true;
                this.WriteState();
            },
            this.WhenAny(
                x => x.IsHostOnline,
                x => !x.GetValue()
            )
        );

        this.StopServer = ReactiveCommand.Create(
            () =>
            {
                if (!this.IsHostOnline)
                    return;

                hostingManager.ClearServices();
                hostingManager.StopAdvertising();
                this.WriteState();
                this.IsHostOnline = false;
            },
            this.WhenAny(
                x => x.IsHostOnline,
                x => x.GetValue()
            )
        );

        this.Clear = ReactiveCommand.Create(() =>
        {
            this.Logs.Clear();
            this.WriteState();
        });
    }


    public ObservableCollection<BleLog> Logs { get; } = new();
    [Reactive] public bool IsHostOnline { get; private set; }
    public ICommand StartServer { get; }
    public ICommand StopServer { get; }
    public ICommand Clear { get; }


    public override void OnAppearing()
        => this.StartServer.Execute(null);


    public override void OnDisappearing()
        => this.StopServer.Execute(null);


    void Log(string @event, byte[]? data = null) => MainThread.BeginInvokeOnMainThread(() =>
    {
        var dataString = String.Empty;
        if (data != null)
            dataString = Encoding.UTF8.GetString(data);

        this.Logs.Add(new BleLog(
            DateTime.Now.ToString("h:mm:ss tt"),
            @event,
            dataString
        ));
    });

    
    void WriteState()
    {
        if (this.IsHostOnline)
        {
            this.Log("BLE Host Online");
            this.Log("Broadcasting Service: " + BleConfiguration.ServiceUuid);            
        }
        else
        {
            this.Log("Stopped BLE Host");
        }
    }
}