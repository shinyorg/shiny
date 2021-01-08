using System;
using System.Reactive.Disposables;
using Shiny.BluetoothLE.Internals;
using Android.Bluetooth;
using Java.Util;
using Observable = System.Reactive.Linq.Observable;


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


        public override IObservable<IGattCharacteristic> DiscoverCharacteristics() => Observable.Create<IGattCharacteristic>(ob =>
        {
            foreach (var characteristic in this.native.Characteristics)
            {
                var wrap = new GattCharacteristic(this, this.context, characteristic);
                ob.OnNext(wrap);
            }
            ob.OnCompleted();
            return Disposable.Empty;
        });


        public override IObservable<IGattCharacteristic> GetKnownCharacteristic(string characteristicUuid)
            => Observable.Create<IGattCharacteristic>(ob =>
            {
                var uuid = UUID.FromString(characteristicUuid);
                var cs = this.native.GetCharacteristic(uuid);
                if (cs == null)
                    throw new ArgumentException("No characteristic found for " + characteristicUuid);

                var characteristic = new GattCharacteristic(this, this.context, cs);
                ob.Respond(characteristic);

                return Disposable.Empty;
            });


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