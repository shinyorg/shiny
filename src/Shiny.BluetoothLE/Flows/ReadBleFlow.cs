using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;


namespace Shiny.BluetoothLE.Flows
{
    public class ReadBleFlow : IBleFlow
    {
        public static IObservable<IBleFlow> Create(IPeripheral peripheral, string serviceUuid, string txCharacteristicUuid, string rxCharacteristicUuid, byte endOfMessage)
            => peripheral
                .GetFlowCharacteristics(serviceUuid, txCharacteristicUuid, rxCharacteristicUuid)
                .Select(chs => new ReadBleFlow(chs.Tx, chs.Rx, endOfMessage))
                .Take(1);


        readonly IGattCharacteristic tx;
        readonly IGattCharacteristic rx;
        readonly byte endOfMessage;


        ReadBleFlow(IGattCharacteristic tx, IGattCharacteristic rx, byte endOfMessage)
        {
            this.tx = tx;
            this.rx = rx;
            this.endOfMessage = endOfMessage;
        }


        public IObservable<byte[]> Write(byte[] data) => Observable.FromAsync(async ct =>
        {
            await this.tx.WriteBlob(new MemoryStream(data)).ToTask(ct);
            var ms = new MemoryStream();
            var eof = false;

            using (var stream = new StreamWriter(ms))
            {
                while (!eof)
                {
                    var result = await this.rx.Read().ToTask(ct);
                    stream.Write(result.Data);
                    eof = result.Data[result.Data.Length] == this.endOfMessage;
                }
            }
            return ms.ToArray();
        });


        public async ValueTask DisposeAsync() { }
    }
}
