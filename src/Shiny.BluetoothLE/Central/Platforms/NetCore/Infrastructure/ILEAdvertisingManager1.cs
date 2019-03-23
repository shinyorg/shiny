using System;
using System.Collections.Generic;
using Tmds.DBus;


namespace Plugin.BluetoothLE.Infrastructure
{
    // on /org/bluez/hciX
    //only appears on adapters that support LE
    [DBusInterface("org.bluez.LEAdvertisingManager1")]
    public interface ILEAdvertisingManager1
    {
        void RegisterAdvertisement(ObjectPath advertisement,IDictionary<string,object> options);
        void UnregisterAdvertisement(ObjectPath service);
    }
}
