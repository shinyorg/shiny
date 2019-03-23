using System;
using Tmds.DBus;


namespace Plugin.BluetoothLE.Infrastructure
{
    [DBusInterface("org.bluez.Agent1")]
    public interface IAlert1
    {
        void Release();
        string RequestPinCode(ObjectPath device);
        void DisplayPinCode(ObjectPath device,string pinCode);
        uint RequestPasskey(ObjectPath device);
        void DisplayPasskey (ObjectPath device, uint passkey, ushort entered);
        void RequestConfirmation(ObjectPath device,uint passkey);
        void RequestAuthorization(ObjectPath device);
        void AuthorizeService(ObjectPath device,string uuid);
        void Cancel();
    }
}
