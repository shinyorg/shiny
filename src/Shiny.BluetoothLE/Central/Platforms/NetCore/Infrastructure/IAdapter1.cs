using System;
using System.Collections.Generic;
using Tmds.DBus;


namespace Plugin.BluetoothLE.Infrastructure
{
    [DBusInterface("org.bluez.Adapter1")]
    public interface IAdapter1
    {
        void RemoveDevice(ObjectPath device);
        void SetDiscoveryFilter(IDictionary<string, object> properties);
        void StartDiscovery();
        void StopDiscovery();

        IList<string> UUIDs { get; }
        bool Discoverable { get; set; }
        bool Discovering { get; }
        bool Pairable { get; set; }
        bool Powered { get; set; }
        string Address { get; }
        string Alias { get; set; }
        string Modalias { get; }
        string Name { get; }
        uint Class { get; }
        uint DiscoverableTimeout { get; set; }
        uint PairableTimeout { get; set; }
    }
}
