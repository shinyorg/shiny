using System;
using Tmds.DBus;


namespace Plugin.BluetoothLE.Infrastructure
{
    // on /org/bluez/hciX/dev_XX_XX_XX_XX_XX_XX/serviceXX/charYYYY
    [DBusInterface("org.bluez.GattCharacteristic1")]
    public interface IGattCharacteristic1
    {
        byte[] ReadValue();
        void WriteValue(byte[] value);
        void StartNotify();
        void StopNotify();

        string UUID { get; }
        ObjectPath Service { get; }
        byte[] Value { get; }
        bool Notifying { get; }
        string[] Flags { get; }
        ObjectPath[] Descriptors { get; }
    }
}
