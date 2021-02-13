using System;
using System.Reactive.Disposables;
using Shiny.BluetoothLE.Internals;
using Android.Bluetooth;
using Java.Util;
using Observable = System.Reactive.Linq.Observable;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Shiny.BluetoothLE
{
    public class GattService : AbstractGattService
    {
        readonly PeripheralContext context;
        readonly BluetoothGattService native;


        public GattService(IPeripheral peripheral,
                           PeripheralContext context,
                           BluetoothGattService native) : base(peripheral,
                                                               native.Uuid.ToString(),
                                                               native.Type == GattServiceType.Primary)
        {
            this.context = context;
            this.native = native;
        }


        public override IObservable<IList<IGattCharacteristic>> GetCharacteristics() =>
            this.native
                .Characteristics
                .Select(native => new GattCharacteristic(this, this.context, native))
                .Cast<IGattCharacteristic>()
                .ToList()
                .Cast<IList<IGattCharacteristic>>()
                .ToObservable();


        public override IObservable<IGattCharacteristic?> GetKnownCharacteristic(string characteristicUuid, bool throwIfNotFound = false)
            => Observable.Create<IGattCharacteristic?>(ob =>
            {
                var uuid = UUID.FromString(characteristicUuid);
                var cs = this.native.GetCharacteristic(uuid);
                if (cs == null)
                    return null;

                var characteristic = new GattCharacteristic(this, this.context, cs);
                ob.Respond(characteristic);

                return Disposable.Empty;
            })
            .Assert(this.Uuid, characteristicUuid, throwIfNotFound);


        public override bool Equals(object obj)
        {
            var other = obj as GattService;
            if (other == null)
                return false;

            if (!Object.ReferenceEquals(this, other))
                return false;

            return true;
        }


        public override int GetHashCode() => this.native.GetHashCode();
        public override string ToString() => $"Peripheral {this.Uuid}";
    }
}