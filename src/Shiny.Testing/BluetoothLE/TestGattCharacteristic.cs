using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shiny.BluetoothLE;


namespace Shiny.Testing.BluetoothLE
{
    public class TestGattCharacteristic : IGattCharacteristic
    {
        public TestGattCharacteristic(string uuid, IGattService service, CharacteristicProperties? props = null)
        {
            this.Uuid = uuid;
            this.Service = service;
            this.Properties = props ?? CharacteristicProperties.Notify | CharacteristicProperties.Write | CharacteristicProperties.Read;
        }


        public IGattService Service { get; }
        public string Uuid { get; }
        public CharacteristicProperties Properties { get; }
        public bool IsNotifying { get; private set; }
        public List<IGattDescriptor> Descriptors { get; set; } = new List<IGattDescriptor>();
        public IObservable<IList<IGattDescriptor>> GetDescriptors() =>
            this.Descriptors
                .Cast<IList<IGattDescriptor>>()
                .ToObservable();


        public IObservable<IGattCharacteristic> EnableNotifications(bool enable, bool useIndicationsIfAvailable)
        {
            this.IsNotifying = enable;
            return Observable.Return(this);
        }


        public Subject<byte[]> NotificationSubject { get; } = new Subject<byte[]>();
        public IObservable<GattCharacteristicResult> WhenNotificationReceived()
            => this.NotificationSubject.Select(data => new GattCharacteristicResult(this, data, GattCharacteristicResultType.Notification));


        public byte[] ReadValue { get; set; }
        public IObservable<GattCharacteristicResult> Read()
            => Observable.Return(new GattCharacteristicResult(this, this.ReadValue, GattCharacteristicResultType.Read));


        public byte[] LastWriteValue { get; private set; }
        public IObservable<GattCharacteristicResult> Write(byte[] value, bool withResponse = true)
        {
            if (value is null)
                throw new ArgumentException("Write value cannot be null", nameof(value));

            if (value.Length > this.Service.Peripheral.MtuSize)
                throw new ArgumentException($"Write length of $'{value.Length}' exceeds MTU size '{this.Service.Peripheral.MtuSize}'");

            this.LastWriteValue = value;
            var type = withResponse ? GattCharacteristicResultType.Write : GattCharacteristicResultType.WriteWithoutResponse;
            return Observable.Return(new GattCharacteristicResult(this, value, type));
        }
    }
}
