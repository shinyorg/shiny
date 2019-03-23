using System;
using Tmds.DBus;


namespace Plugin.BluetoothLE.Infrastructure
{
    // on /org/bluez/hciX/dev_XX_XX_XX_XX_XX_XX/serviceXX/charYYYY/descriptorZZZ
    [DBusInterface("org.bluez.GattDescriptor1")]
    public interface IGattDescriptor1
    {
        byte[] ReadValue();
        void WriteValue(byte[] value);

        string UUID { get; }
        ObjectPath Characteristic { get; }
        byte[] Value { get; }
        string[] Flags { get; }
    }
}
