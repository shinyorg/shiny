using System;
using System.Collections.Generic;
using System.Linq;
using Android.Bluetooth;
using Shiny.BluetoothLE.Peripherals.Internals;


namespace Shiny.BluetoothLE.Hosting
{
    public class GattService : IGattService, IGattServiceBuilder, IDisposable
    {
        readonly GattServerContext context;
        readonly List<GattCharacteristic> characteristics;


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


        public void Dispose()
        {
            foreach (var ch in this.characteristics)
                ch.Dispose();
        }
    }
}