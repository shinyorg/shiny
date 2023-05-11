using System;
using System.Text;
using DynamicData;
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

                        var notifier = sb.AddCharacteristic(BleConfiguration.NotifyCharacteristicUuid, x => x.SetNotification(async sub =>
                        {
                            var smsg = sub.IsSubscribing ? "Subscribed" : "UnSubscribed";
                            await this.Log($"{sub.Peripheral.Uuid} {smsg} to Characteristic");
                        }));

                        sb.AddCharacteristic(BleConfiguration.ReadCharacteristicUuid, x => x.SetRead(async request =>
                        {
                            var data = currentData ?? new byte[] { 0x0 };
                            await this.Log($"{request.Peripheral.Uuid} Read Characteristic", data);

                            return GattResult.Success(data);
                        }));

                        sb.AddCharacteristic(BleConfiguration.WriteCharacteristicUuid, cb => cb.SetWrite(async request =>
                        {
                            currentData = request.Data;
                            await this.Log($"{request.Peripheral.Uuid} Wrote to Characteristic", request.Data);

                            if (notifier.SubscribedCentrals.Count > 0)
                            {
                                await notifier.Notify(request.Data);
                                await this.Log("Notification Broadcasted to subscribers");
                            }
                        }, WriteOptions.Write | WriteOptions.WriteWithoutResponse));
                    }
                );
                await hostingManager.AddService(
                    BleConfiguration.SecondaryServiceUuid,
                    false,
                    sb =>
                    {
                        sb.AddCharacteristic(BleConfiguration.SecondaryCharacteristicUuid1, x => x.SetRead(async request =>
                        {
                            await this.Log("Secondary Read - CH1");
                            return GattResult.Success(new byte[] { 0x01 });
                        }));

                        sb.AddCharacteristic(BleConfiguration.SecondaryCharacteristicUuid2, x => x.SetRead(async request =>
                        {
                            await this.Log("Secondary Read - CH2");
                            return GattResult.Success(new byte[] { 0x01 });
                        }));
                    }
                );
                await hostingManager.StartAdvertising(new(ServiceUuids: new[]
                {
                    BleConfiguration.ServiceUuid,
                    BleConfiguration.SecondaryServiceUuid
                }));
                await this.Log("BLE Host Online");
                await this.Log("Broadcasting Service: " + BleConfiguration.ServiceUuid);
                await this.Log("Broadcasting Service: " + BleConfiguration.SecondaryServiceUuid);
                this.IsHostOnline = true;
            },
            this.WhenAny(
                x => x.IsHostOnline,
                x => !x.GetValue()
            )
        );

        this.StopServer = ReactiveCommand.CreateFromTask(
            async () =>
            {
                if (!this.IsHostOnline)
                    return;

                hostingManager.ClearServices();
                hostingManager.StopAdvertising();
                await this.Log("Stopped BLE Host");
                this.IsHostOnline = false;
            },
            this.WhenAny(
                x => x.IsHostOnline,
                x => x.GetValue()
            )
        );
    }


    public ObservableCollection<BleLog> Logs { get; } = new();
    [Reactive] public bool IsHostOnline { get; private set; }
    public ICommand StartServer { get; }
    public ICommand StopServer { get; }


    public override void OnAppearing()
        => this.StartServer.Execute(null);


    public override void OnDisappearing()
        => this.StopServer.Execute(null);


    Task Log(string @event, byte[]? data = null) => MainThread.InvokeOnMainThreadAsync(() =>
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
}