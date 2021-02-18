using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;


namespace Shiny.BluetoothLE.Flows
{
    public class NotifyBleFlow : IBleFlow
    {
        public static IObservable<IBleFlow> Create(IPeripheral peripheral, string serviceUuid, string txCharacteristicUuid, string rxCharacteristicUuid, byte endOfMessage)
            => Observable.FromAsync<IBleFlow>(async ct =>
            {
                var chs = await peripheral.GetFlowCharacteristics(serviceUuid, txCharacteristicUuid, rxCharacteristicUuid);
                await chs.Rx.EnableNotifications(true);
                return new NotifyBleFlow(chs.Tx, chs.Rx, endOfMessage);
            });


        readonly IGattCharacteristic tx;
        readonly IGattCharacteristic rx;
        readonly byte endOfMessage;


        NotifyBleFlow(IGattCharacteristic tx, IGattCharacteristic rx, byte endOfMessage)
        {
            this.tx = tx;
            this.rx = rx;
            this.endOfMessage = endOfMessage;
        }


        public IObservable<byte[]> Write(byte[] data) => Observable.Create<byte[]>(async ob =>
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);

            var sub = this.rx
                .Notify(false)
                .Subscribe(result =>
                {
                    sw.Write(result.Data);
                    if (result.Data[result.Data.Length] == this.endOfMessage)
                        ob.Respond(ms.ToArray());
                });

            try
            {
                await this.tx.WriteBlob(new MemoryStream(data));
            }
            catch (Exception ex)
            {
                ob.OnError(ex);
            }

            return () =>
            {
                sub.Dispose();
                sw.Dispose();
            };
        });


        public async ValueTask DisposeAsync() => await this.rx.EnableNotifications(false);
    }
}
