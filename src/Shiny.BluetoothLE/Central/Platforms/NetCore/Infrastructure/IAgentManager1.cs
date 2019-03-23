using System;
using Tmds.DBus;


namespace Plugin.BluetoothLE.Infrastructure
{
    [DBusInterface("org.bluez.AgentManager1")]
    public interface IAgentManager1
    {
        void RegisterAgent(ObjectPath agent,string capability);
        void RequestDefaultAgent(ObjectPath agent);
        void UnregisterAgent(ObjectPath agent);
    }
}
