using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CoreBluetooth;


namespace Shiny.BluetoothLE
{
    public class GattService : AbstractGattService
    {
        public CBService Service { get; }
        public CBPeripheral Peripherial { get; }


        public GattService(Peripheral peripheral, CBService native)
            : base(peripheral, native.UUID.ToString(), native.Primary)
        {
            this.Peripherial = peripheral.Native;
            this.Service = native;
        }


        public override IObservable<IGattCharacteristic?> GetKnownCharacteristic(string characteristicUuid, bool throwIfNotFound = false)
            => Observable.Create<IGattCharacteristic?>(ob =>
            {
                var characteristic = this.Service
                    .Characteristics?
                    .Select(ch => new GattCharacteristic(this, ch))
                    .FirstOrDefault(ch => ch.Uuid.Equals(characteristicUuid, StringComparison.InvariantCultureIgnoreCase));

                if (characteristic != null)
                {
                    ob.Respond(characteristic);
                    return Disposable.Empty;
                }

                var characteristics = new Dictionary<string, IGattCharacteristic>();
                var handler = new EventHandler<CBServiceEventArgs>((sender, args) =>
                {
                    if (this.Service.Characteristics == null)
                        return;

                    if (!this.Equals(args.Service))
                        return;

                    var characteristic = this.Service
                        .Characteristics?
                        .Select(ch => new GattCharacteristic(this, ch))
                        .FirstOrDefault(ch => ch.Uuid.Equals(characteristicUuid, StringComparison.InvariantCultureIgnoreCase));

                     ob.Respond(characteristic);
                });
                var nativeUuid = CBUUID.FromString(characteristicUuid);
                this.Peripherial.DiscoveredCharacteristic += handler;
                this.Peripherial.DiscoverCharacteristics(new [] { nativeUuid }, this.Service);

                return Disposable.Create(() => this.Peripherial.DiscoveredCharacteristic -= handler);
            })
            .Assert(this.Uuid, characteristicUuid, throwIfNotFound);


        public override IObservable<IList<IGattCharacteristic>> GetCharacteristics()
            => Observable.Create<IList<IGattCharacteristic>>(ob =>
            {
                var characteristics = new Dictionary<string, IGattCharacteristic>();
                var handler = new EventHandler<CBServiceEventArgs>((sender, args) =>
                {
                    if (this.Service.Characteristics == null)
                        return;

                    if (!this.Equals(args.Service))
                        return;

                    var list = this.Service
                        .Characteristics
                        .Select(ch => new GattCharacteristic(this, ch))
                        .Distinct()
                        .Cast<IGattCharacteristic>()
                        .ToList();

                    ob.Respond(list);
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
