//using System;
//using System.Reactive.Subjects;
//using Shiny.BluetoothLE.Hosting;
//using Shiny.BluetoothLE.Hosting.Managed;
//using Shiny.Tests.BluetoothLE;

//namespace Sample.Dev;


//public record BleLog(
//    DateTime Timestamp,
//    string Event,
//    byte[]? Data
//);

//[BleGattCharacteristic(BleConfiguration.ServiceUuid, BleConfiguration.ReadCharacteristicUuid)]
//public class UnitTestBleCharacteristic : BleGattCharacteristic
//{
//    readonly Subject<BleLog> logSubj = new();
//    byte[] currentData = new byte[] { 0x0 };

//    public IObservable<BleLog> WhenLogReceived() => this.logSubj;

//    public override Task<GattResult> OnRead(ReadRequest request) => base.OnRead(request);
//    public override Task OnWrite(WriteRequest request) => base.OnWrite(request);
//    public override Task OnSubscriptionChanged(IPeripheral peripheral, bool subscribed) => base.OnSubscriptionChanged(peripheral, subscribed);

//    //byte[]? currentData = null;

//    //var notifier = sb.AddCharacteristic(BleConfiguration.NotifyCharacteristicUuid, x => x.SetNotification(async sub =>
//    //{
//    //    var smsg = sub.IsSubscribing ? "Subscribed" : "UnSubscribed";
//    //    await this.Log($"{sub.Peripheral.Uuid} {smsg} to Characteristic");
//    //}));

//    //sb.AddCharacteristic(BleConfiguration.ReadCharacteristicUuid, x => x.SetWrite(async request =>
//    //            {
//    //                var data = currentData ?? new byte[] { 0x0 };
//    //await this.Log($"{request.Peripheral.Uuid} Read Characteristic", data);
//    //}));

//    //            sb.AddCharacteristic(BleConfiguration.WriteCharacteristicUuid, cb => cb.SetWrite(async request =>
//    //            {
//    //                currentData = request.Data;
//    //                await this.Log($"{request.Peripheral.Uuid} Wrote to Characteristic", request.Data);

//    //await notifier.Notify(request.Data);
//    //            }));
//}