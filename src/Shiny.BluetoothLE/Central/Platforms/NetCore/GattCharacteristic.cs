using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DBus;
using Mono.BlueZ.DBus;
using org.freedesktop.DBus;


namespace Plugin.BluetoothLE
{
    public class GattCharacteristic : AbstractGattCharacteristic
    {
        readonly GattCharacteristic1 native;


        public GattCharacteristic(GattCharacteristic1 native, IGattService service, CharacteristicProperties properties)
            : base(service, Guid.Parse(native.UUID), properties)
        {
            this.native = native;
        }


        //public override IObservable<bool> SetNotificationValue(CharacteristicConfigDescriptorValue value) => Observable.Create<bool>(ob =>
        //{
        //    switch (value)
        //    {
        //        case CharacteristicConfigDescriptorValue.None:
        //            this.native.StopNotify();
        //            break;

        //        default:
        //            this.native.StartNotify();
        //            break;
        //    }
        //    return Disposable.Empty;
        //});


        public override IObservable<bool> EnableNotifications(bool enableIndicationsIfAvailable)
        {
            throw new NotImplementedException();
        }


        public override IObservable<object> DisableNotifications()
        {
            throw new NotImplementedException();
        }


        public override IObservable<CharacteristicResult> WhenNotificationReceived() => Observable.Create<CharacteristicResult>(ob =>
        {
            var readCharPath = new ObjectPath("/org/bluez/hci0/dev_F6_58_7F_09_5D_E6/service000c/char000f");
            var readChar = Bus.System.GetObject<GattCharacteristic1>(BlueZPath.Service, readCharPath);
            var properties = Bus.System.GetObject<Properties>(BlueZPath.Service, readCharPath);

            var handler = new PropertiesChangedHandler((@interface, changed, invalidated) =>
            {
                if (changed != null)
                {
                    foreach (var prop in changed.Keys)
                    {
                        if (changed[prop] is byte[])
                        {

                        }
                    }
                }
            });
            properties.PropertiesChanged += handler;

            return () =>
            {
                properties.PropertiesChanged -= handler;
                //agentManager.UnregisterAgent (agentPath);
                //gattManager.UnregisterProfile (gattProfilePath);
            };
        });


        public override IObservable<IGattDescriptor> WhenDescriptorDiscovered() => Observable.Create<IGattDescriptor>(ob =>
        {
            // TODO: refresh per connection
            foreach (var path in this.native.Descriptors)
            {
                var desc = Bus.System.GetObject<GattDescriptor1>(BlueZPath.Service, path);
                var acr = new GattDescriptor(desc, this);
                ob.OnNext(acr);
            }
            return Disposable.Empty;
        });


        public override void WriteWithoutResponse(byte[] value) => this.native.WriteValue(value);


        public override IObservable<CharacteristicResult> Write(byte[] value) => Observable.Create<CharacteristicResult>(ob =>
        {
            this.native.WriteValue(value);
            var result = new CharacteristicResult(this, CharacteristicEvent.Write, value);
            ob.Respond(result);
            return Disposable.Empty;
        });


        public override IObservable<CharacteristicResult> Read() => Observable.Create<CharacteristicResult>(ob =>
        {
            var data = this.native.ReadValue();
            var result = new CharacteristicResult(this, CharacteristicEvent.Read, data);
            ob.Respond(result);
            return Disposable.Empty;
        });
    }
}