using System;
using System.Collections.Generic;
using System.Linq;
using CoreBluetooth;


namespace Shiny.BluetoothLE.Peripherals
{
    public class GattService : IGattService, IGattServiceBuilder, IDisposable
    {
        readonly CBPeripheralManager manager;
        readonly IList<GattCharacteristic> characteristics;


        public GattService(CBPeripheralManager manager, Guid uuid, bool primary)
        {
            this.manager = manager;
            this.Native = new CBMutableService(uuid.ToCBUuid(), primary);
            this.characteristics = new List<GattCharacteristic>();
        }


        public CBMutableService Native { get; }
        public Guid Uuid => this.Native.UUID.ToGuid();
        public bool Primary => this.Native.Primary;
        public IReadOnlyList<IGattCharacteristic> Characteristics => this.characteristics.Cast<IGattCharacteristic>().ToList();


        public IGattCharacteristic AddCharacteristic(Guid uuid, Action<IGattCharacteristicBuilder> characteristicBuilder)
        {
            var ch = new GattCharacteristic(this.manager, uuid);
            characteristicBuilder(ch);
            ch.Build(this.Native);

            this.characteristics.Add(ch);
            return ch;
        }


        public void Dispose()
        {
            foreach (var ch in this.characteristics)
                ch.Dispose();
        }
    }
}
