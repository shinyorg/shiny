using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Central;


namespace Shiny.Testing.BluetoothLE.Central
{
    public class MockGattCharacteristic : AbstractGattCharacteristic
    {
        public MockGattCharacteristic(IGattService service, Guid guid, CharacteristicProperties properties)
            : base(service, guid, properties)
        {
        }

        public byte[] ReturnValue { get; set; }
        public override byte[] Value => this.ReturnValue;


        public IList<IGattDescriptor> GattDescriptors { get; set; } = new List<IGattDescriptor>();
        public override IObservable<IGattDescriptor> DiscoverDescriptors() => this.GattDescriptors.ToObservable();


        public override IObservable<CharacteristicGattResult> EnableNotifications(bool enableIndicationsIfAvailable)
            => Observable.Return(new CharacteristicGattResult(this, null));
        public override IObservable<CharacteristicGattResult> DisableNotifications()
            => Observable.Return(new CharacteristicGattResult(this, null));


        public byte[] ReadBytes { get; set; }
        public override IObservable<CharacteristicGattResult> Read()
            => Observable.Return(new CharacteristicGattResult(this, this.ReadBytes));


        public Subject<byte[]> NotificationSubject { get; } = new Subject<byte[]>();
        public override IObservable<CharacteristicGattResult> WhenNotificationReceived()
            => this.NotificationSubject.Select(x => new CharacteristicGattResult(this, x));


        public override IObservable<CharacteristicGattResult> Write(byte[] value, bool withResponse)
        {
            this.ReturnValue = value;
            return Observable.Return(new CharacteristicGattResult(this, value));
        }
    }
}
