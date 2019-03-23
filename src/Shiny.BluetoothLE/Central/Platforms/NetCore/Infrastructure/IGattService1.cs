using System;
using Tmds.DBus;


namespace Plugin.BluetoothLE.Infrastructure
{
    // on /org/bluez/hciX/dev_XX_XX_XX_XX_XX_XX/serviceXX
    [DBusInterface("org.bluez.GattService1")]
    public interface IGattService1
    {
        string UUID { get; }
        bool Primary { get; }
        ObjectPath Device { get; }
        ObjectPath[] Characteristics { get; }
        ObjectPath[] Includes { get; }
    }
}
