using System;
using System.Linq;
using System.Reactive.Linq;
using Shiny.BluetoothLE.Flows;


namespace Shiny.BluetoothLE
{
    public static class FlowExtensions
    {
        public static IObservable<(IGattCharacteristic Tx, IGattCharacteristic Rx)> GetFlowCharacteristics(this IPeripheral peripheral, string serviceUuid, string txCharacteristicUuid, string rxCharacteristicUuid) => peripheral
            .GetKnownCharacteristic(serviceUuid, txCharacteristicUuid)
            .Zip(peripheral.GetKnownCharacteristic(serviceUuid, rxCharacteristicUuid));

        // TODO: flow control - no end of message - both notify & rx

        public static IObservable<IBleFlow> NotifyFlow(this IPeripheral peripheral, string serviceUuid, string txCharacteristicUuid, string rxCharacteristicUuid, byte endOfMessage)
            => NotifyBleFlow.Create(peripheral, serviceUuid, txCharacteristicUuid, rxCharacteristicUuid, endOfMessage);


        public static IObservable<IBleFlow> ReadFlow(this IPeripheral peripheral, string serviceUuid, string txCharacteristicUuid, string rxCharacteristicUuid, byte endOfMessage)
            => ReadBleFlow.Create(peripheral, serviceUuid, txCharacteristicUuid, rxCharacteristicUuid, endOfMessage);
    }
}