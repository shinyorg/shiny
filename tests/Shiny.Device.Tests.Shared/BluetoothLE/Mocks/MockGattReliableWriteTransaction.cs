using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using Shiny.BluetoothLE.Central;


namespace Shiny.Devices.Tests.BluetoothLE.Mocks
{
    public class MockGattReliableWriteTransaction : IGattReliableWriteTransaction
    {
        public TransactionState Status => TransactionState.Active;

        public List<byte[]> WrittenValues { get; set; } = new List<byte[]>();

        public void Abort()
        {
        }

        public IObservable<Unit> Commit()
        {
            return Observable.Create<Unit>(obs =>
            {
                obs.OnCompleted();
                return () => { };
            });
        }

        public void Dispose()
        {
        }

        public IObservable<CharacteristicGattResult> Write(IGattCharacteristic characteristic, byte[] value)
        {
            this.WrittenValues.Add(value.ToArray());
            return characteristic.Write(value);
        }
    }
}
