using System;
using System.Collections.Generic;
using System.Linq;
using Android.Bluetooth;
using Shiny.BluetoothLE.Peripherals.Internals;


namespace Shiny.BluetoothLE.Peripherals
{
    public class GattService : IGattService, IGattServiceBuilder
    {
        readonly GattServerContext context;
        readonly List<GattCharacteristic> characteristics;
        readonly BluetoothGattService native;


        public GattService(GattServerContext context, Guid uuid, bool primary)
        {
            this.context = context;
            var type = primary ? GattServiceType.Primary : GattServiceType.Secondary;
            this.Native = new BluetoothGattService(uuid.ToUuid(), type);
            this.characteristics = new List<GattCharacteristic>();
        }


        public BluetoothGattService Native { get; }

        public Guid Uuid => this.Native.Uuid.ToGuid();
        public bool Primary => this.Native.Type == GattServiceType.Primary;
        public IReadOnlyList<IGattCharacteristic> Characteristics =>
            this.characteristics.Cast<IGattCharacteristic>().ToArray();


        public IGattCharacteristic AddCharacteristic(Guid uuid, Action<IGattCharacteristicBuilder> characteristicBuilder)
        {
            var ch = new GattCharacteristic(this.context, uuid);
            characteristicBuilder(ch);
            ch.Build();
            this.Native.AddCharacteristic(ch.Native);
            this.characteristics.Add(ch);
            return ch;
        }
    }
}