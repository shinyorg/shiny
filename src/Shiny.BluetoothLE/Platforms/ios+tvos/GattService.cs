using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using CoreBluetooth;


namespace Shiny.BluetoothLE
{
    public class GattService : AbstractGattService
    {
        public CBService Service { get; }
        public CBPeripheral Peripherial { get; }


        public GattService(Peripheral peripheral, CBService native) : base(peripheral, native.UUID.ToString(), native.Primary)
        {
            this.Peripherial = peripheral.Native;
            this.Service = native;
        }


        public override IObservable<IGattCharacteristic> GetKnownCharacteristic(string characteristicUuid)
            => Observable.Create<IGattCharacteristic>(ob =>
            {
                var uuid = CBUUID.FromString(characteristicUuid);
                var characteristics = new Dictionary<string, IGattCharacteristic>();
                var handler = new EventHandler<CBServiceEventArgs>((sender, args) =>
                {
                    if (this.Service.Characteristics == null)
                        return;

                    if (!this.Equals(args.Service))
                        return;

                    var native = this.Service.Characteristics.FirstOrDefault(x => x.UUID.Equals(uuid));
                    if (native == null)
                        ob.OnError(new ArgumentException("No characteristic found for " + characteristicUuid));

                    ob.Respond(new GattCharacteristic(this, native));
                });
                this.Peripherial.DiscoveredCharacteristic += handler;
                this.Peripherial.DiscoverCharacteristics(new [] { uuid }, this.Service);

                return () => this.Peripherial.DiscoveredCharacteristic -= handler;
            });


        public override IObservable<IGattCharacteristic> DiscoverCharacteristics()
            => Observable.Create<IGattCharacteristic>(ob =>
            {
                var characteristics = new Dictionary<string, IGattCharacteristic>();
                var handler = new EventHandler<CBServiceEventArgs>((sender, args) =>
                {
                    if (this.Service.Characteristics == null)
                        return;

                    if (!this.Equals(args.Service))
                        return;

                    foreach (var nch in this.Service.Characteristics)
                    {
                        var ch = new GattCharacteristic(this, nch);
                        if (!characteristics.ContainsKey(ch.Uuid))
                        {
                            characteristics.Add(ch.Uuid, ch);
                            ob.OnNext(ch);
                        }
                    }
                    ob.OnCompleted();
                });
                this.Peripherial.DiscoveredCharacteristic += handler;
                this.Peripherial.DiscoverCharacteristics(this.Service);

                return () => this.Peripherial.DiscoveredCharacteristic -= handler;
            });


        bool Equals(CBService service)
        {
            if (!this.Service.UUID.Equals(service.UUID))
                return false;

			if (!this.Peripherial.Identifier.Equals(service.Peripheral.Identifier))
                return false;

            return true;
        }


        public override bool Equals(object obj)
        {
            var other = obj as GattService;
            if (other == null)
                return false;

            if (!this.Service.Peripheral.IsEqual(other.Service.Peripheral))
                return false;

            if (!this.Service.UUID.Equals(other.Service.UUID))
                return false;

            return true;
        }


        public override int GetHashCode() => this.Service.GetHashCode();
        public override string ToString() => this.Uuid.ToString();
    }
}
