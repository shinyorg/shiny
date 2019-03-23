using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DBus;
using Mono.BlueZ.DBus;


namespace Plugin.BluetoothLE
{
    public class GattService : AbstractGattService
    {
        readonly GattService1 native;


        public GattService(GattService1 native, IDevice device) : base(device, Guid.Parse(native.UUID), native.Primary)
        {
            this.native = native;
        }


        public override IObservable<IGattCharacteristic> WhenCharacteristicDiscovered() => Observable.Create<IGattCharacteristic>(ob =>
        {
            // TODO: refresh per connection
            foreach (var path in this.native.Characteristics)
            {
                var ch = Bus.System.GetObject<GattCharacteristic1>(BlueZPath.Service, path);
                var acr = new GattCharacteristic(ch, this, CharacteristicProperties.Read); // TODO
                ob.OnNext(acr);
            }
            return Disposable.Empty;
        });
    }
}
