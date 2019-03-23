using System;
using System.Collections.Generic;
using Tmds.DBus;


namespace Plugin.BluetoothLE.Infrastructure
{
    // on /org/bluez/hciX
    //appears on all adapters, even ones that don't do LE.... which is kindof odd
    [DBusInterface("org.bluez.GattManager1")]
    public interface IGattManager1
    {
        void RegisterProfile(ObjectPath profile, string[] UUIDs, IDictionary<string, object> options);
        void RegisterService(ObjectPath service, IDictionary<string, object> options);
        void UnregisterProfile(ObjectPath profile);
        void UnregisterService(ObjectPath service);
    }
}
