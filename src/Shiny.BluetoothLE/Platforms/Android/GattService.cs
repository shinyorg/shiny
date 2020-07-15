using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Shiny.BluetoothLE.Internals;
using Android.Bluetooth;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE
{
    public class GattService : AbstractGattService
    {
        readonly DeviceContext context;
        readonly BluetoothGattService native;


        public GattService(IPeripheral peripheral,
                           DeviceContext context,
                           BluetoothGattService native) : base(peripheral,
                                                               native.Uuid.ToGuid(),
                                                               native.Type == GattServiceType.Primary)
        {
            this.context = context;
            this.native = native;
        }


        public override IObservable<IGattCharacteristic> DiscoverCharacteristics()
        {
            var discoverCharacteristics = new Subject<IGattCharacteristic>();
            Task.Run(() =>
            {
                try
                {
                    foreach (var characteristic in this.native.Characteristics)
                    {
                        var wrap = new GattCharacteristic(this, this.context, characteristic);
                        discoverCharacteristics.OnNext(wrap);
                    }
                    discoverCharacteristics.OnCompleted();
                }
                catch (Exception ex) { discoverCharacteristics.OnError(ex); }
            });
            return discoverCharacteristics.AsObservable();
        }

        public override IObservable<IGattCharacteristic> GetKnownCharacteristics(params Guid[] characteristicIds)
            => Observable.Create<IGattCharacteristic>(ob =>
            {
                var cids = characteristicIds.Select(x => x.ToUuid()).ToArray();
                foreach (var cid in cids)
                {
                    var cs = this.native.GetCharacteristic(cid);
                    var characteristic = new GattCharacteristic(this, this.context, cs);
                    ob.OnNext(characteristic);
                }
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