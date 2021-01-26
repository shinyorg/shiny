//using System;
//using System.Reactive.Linq;


//namespace Shiny.BluetoothLE
//{
//    public interface IBleFlow
//    {
//        IObservable<byte[]> Write(byte[] data);
//    }


//    public static class FlowExtensions
//    {
//        public static IBleFlow CreateFlow(this IPeripheral peripheral, string serviceUuid, string txCharacteristicUuid, string rxCharacteristicUuid, byte endOfMessage = 0x0, bool useNotifications = true)
//            => useNotifications
//                ? new NotifyBleFlow(peripheral, serviceUuid, txCharacteristicUuid, rxCharacteristicUuid, endOfMessage)
//                : new ReadBleFlow(peripheral, serviceUuid, txCharacteristicUuid, rxCharacteristicUuid, endOfMessage);
//    }


//    public class ReadBleFlow : AbstractBleFlow
//    {
//        public ReadBleFlow(
//            IPeripheral peripheral,
//            string serviceUuid,
//            string txCharacteristicUuid,
//            string rxCharacteristicUuid,
//            byte endOfMessage)
//        : base(
//            peripheral,
//            serviceUuid,
//            txCharacteristicUuid,
//            rxCharacteristicUuid,
//            endOfMessage
//        )
//        { }


//        public override IObservable<byte[]> Write(byte[] data) => null;
//    }


//    public class NotifyBleFlow : AbstractBleFlow
//    {
//        public NotifyBleFlow(
//            IPeripheral peripheral,
//            string serviceUuid,
//            string txCharacteristicUuid,
//            string rxCharacteristicUuid,
//            byte endOfMessage)
//        : base(
//            peripheral,
//            serviceUuid,
//            txCharacteristicUuid,
//            rxCharacteristicUuid,
//            endOfMessage
//        )
//        { }


//        public override IObservable<byte[]> Write(byte[] data) => null;
//    }


//    public abstract class AbstractBleFlow : IBleFlow
//    {
//        protected AbstractBleFlow(IPeripheral peripheral,
//                                  string serviceUuid,
//                                  string txCharacteristicUuid,
//                                  string rxCharacteristicUuid,
//                                  byte endOfMessage)
//        {
//            this.Peripheral = peripheral;
//            this.ServiceUuid = serviceUuid;
//            this.TxCharacteristicUuid = txCharacteristicUuid;
//            this.RxCharacteristicUuid = rxCharacteristicUuid;
//            this.EndOfMessage = endOfMessage;
//        }


//        IGattCharacteristic? tx;
//        IGattCharacteristic? rx;
//        public virtual IObservable<byte[]> Write(byte[] data) => Observable.Create(ob =>
//        {
//            this.tx ??= await this.Peripheral.GetKnownCharacteristicAsync(this.ServiceUuid, this.TxCharacteristicUuid);
//            this.rx ??= await this.Peripheral.GetKnownCharacteristicAsync(this.ServiceUuid, this.RxCharacteristicUuid);

//            return () =>
//            {
//            };
//        });


//        protected abstract IObservable<byte[]> GetData()


//        public IPeripheral Peripheral { get; }
//        public string ServiceUuid { get; }
//        public string TxCharacteristicUuid { get; }
//        public string RxCharacteristicUuid { get; }
//        public byte EndOfMessage { get; }
//    }
//}
