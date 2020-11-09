using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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


        public override IObservable<IGattCharacteristic> GetKnownCharacteristics(params string[] characteristicIds)
            => Observable.Create<IGattCharacteristic>(ob =>
            {
                var cids = characteristicIds.Select(UUID.FromString).ToArray();
                var found = false;

                foreach (var cid in cids)
                {
                    var cs = this.native.GetCharacteristic(cid);
                    if (cs != null)
                    {
                        found = true;
                        var characteristic = new GattCharacteristic(this, this.context, cs);
                        ob.OnNext(characteristic);
                    }
                }
                if (!found)
                    throw new ArgumentException("No characteristics found for UUID list");

                ob.OnCompleted();

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