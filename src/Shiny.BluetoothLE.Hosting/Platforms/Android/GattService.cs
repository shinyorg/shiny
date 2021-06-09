using System;
using System.Collections.Generic;
using System.Linq;
using Android.Bluetooth;
using Java.Util;
using Shiny.BluetoothLE.Hosting.Internals;


namespace Shiny.BluetoothLE.Hosting
{
    public class GattService : IGattService, IGattServiceBuilder, IDisposable
    {
        readonly GattServerContext context;
        readonly List<GattCharacteristic> characteristics;


        public GattService(GattServerContext context, string uuid, bool primary)
        {
            this.context = context;
            var type = primary ? GattServiceType.Primary : GattServiceType.Secondary;

            this.Native = new BluetoothGattService(Utils.ToUuidType(uuid), type);
            this.characteristics = new List<GattCharacteristic>();

            this.Uuid = uuid;
            this.Primary = primary;
        }


        public BluetoothGattService Native { get; }

        public string Uuid { get; }
        public bool Primary { get; }
        public IReadOnlyList<IGattCharacteristic> Characteristics =>
            this.characteristics.Cast<IGattCharacteristic>().ToArray();


        public IGattCharacteristic AddCharacteristic(string uuid, Action<IGattCharacteristicBuilder> characteristicBuilder)
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