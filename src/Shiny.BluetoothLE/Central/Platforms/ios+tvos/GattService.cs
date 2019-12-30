using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using CoreBluetooth;


namespace Shiny.BluetoothLE.Central
{
    public class GattService : AbstractGattService
    {
        public CBService Service { get; }
        public CBPeripheral Peripherial { get; }


        public GattService(Peripheral peripheral, CBService native) : base(peripheral, native.UUID.ToGuid(), native.Primary)
        {
            this.Peripherial = peripheral.Native;
            this.Service = native;
        }


        public override IObservable<IGattCharacteristic> GetKnownCharacteristics(params Guid[] characteristicIds)
            => Observable.Create<IGattCharacteristic>(ob =>
            {
                var characteristics = new Dictionary<Guid, IGattCharacteristic>();
                var handler = new EventHandler<CBServiceEventArgs>((sender, args) =>
                {
                    if (this.Service.Characteristics == null)
                        return;

                    if (!this.Equals(args.Service))
                        return;

                    foreach (var nch in this.Service.Characteristics)
                    {
                        var ch = new GattCharacteristic(this, nch);
                        if (!characteristics.ContainsKey(ch.Uuid) && characteristicIds.Any(x => ch.Uuid == x))
                        {
                            characteristics.Add(ch.Uuid, ch);
                            ob.OnNext(ch);
                        }
                    }
                    if (characteristics.Count == characteristicIds.Length)
                        ob.OnCompleted();
                });
                var uuids = characteristicIds.Select(x => x.ToCBUuid()).ToArray();
                this.Peripherial.DiscoveredCharacteristic += handler;
                this.Peripherial.DiscoverCharacteristics(uuids, this.Service);

                return () => this.Peripherial.DiscoveredCharacteristic -= handler;
            });


        public override IObservable<IGattCharacteristic> DiscoverCharacteristics()
            => Observable.Create<IGattCharacteristic>(ob =>
            {
                var characteristics = new Dictionary<Guid, IGattCharacteristic>();
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
